using TdLib;
using static TdLib.TdApi;

namespace TelegramShellClient
{
    internal class Authorization : Dialog
    {
        private static readonly Authorization? instance = null;

        private readonly SetParameters         setParameters;
        private readonly SetEmailAdressAsync setEmailAdressAsync;
        private readonly RequestQrCodeAsync    requestQrCodeAsync;
        private readonly SetPhoneNumberAsync   setPhoneNumberAsync;
        private readonly CheckCodeAsync        checkCodeAsync;
        private readonly ResendCodeAsync       resendCodeAsync;
        private readonly CheckEmailCode        checkEmailCodeAsync;
        private readonly CheckPasswordAsync    checkPasswordAsync;


        public delegate Task<Ok> SetParameters();
        public delegate Task<Ok> SetEmailAdressAsync(string email);
        public delegate Task<Ok> RequestQrCodeAsync();
        public delegate Task<Ok> SetPhoneNumberAsync(string phone, PhoneNumberAuthenticationSettings settings);
        public delegate Task<Ok> CheckCodeAsync(string code);
        public delegate Task<Ok> ResendCodeAsync();
        public delegate Task<Ok> CheckEmailCode(EmailAddressAuthentication.EmailAddressAuthenticationCode code);
        public delegate Task<Ok> CheckPasswordAsync(string password);

        public bool IsAuthorized { get; private set; }
        public bool IsLoggingOut { get; private set; }

        private bool resend_flag = false;


        private readonly Updatable<AuthorizationState, Update.UpdateAuthorizationState> state = new( 
            delegate (Update.UpdateAuthorizationState update) 
            { 
                return update.AuthorizationState; 
            });

        private Authorization(SetParameters setParameters, SetEmailAdressAsync setEmailAdressAsync, RequestQrCodeAsync requestQrCodeAsync, SetPhoneNumberAsync setPhoneNumberAsync, 
            CheckCodeAsync checkCodeAsync, ResendCodeAsync resendCodeAsync, CheckEmailCode checkEmailCodeAsync, CheckPasswordAsync checkPasswordAsync) : base(10)
        {
            this.setParameters = setParameters;
            this.setEmailAdressAsync = setEmailAdressAsync;
            this.requestQrCodeAsync = requestQrCodeAsync;
            this.checkCodeAsync = checkCodeAsync;
            this.resendCodeAsync = resendCodeAsync;
            this.setPhoneNumberAsync = setPhoneNumberAsync;
            this.checkEmailCodeAsync = checkEmailCodeAsync;
            this.checkPasswordAsync = checkPasswordAsync;

            state.Notify += OnStateUpdated;
        }

        public static Authorization GetInstance(SetParameters setParameters, SetEmailAdressAsync setEmailAdressAsync, RequestQrCodeAsync requestQrCodeAsync, SetPhoneNumberAsync setPhoneNumberAsync,
            CheckCodeAsync checkCodeAsync, ResendCodeAsync resendCodeAsync, CheckEmailCode checkEmailCodeAsync, CheckPasswordAsync checkPasswordAsync)
        {
            return instance ?? new Authorization(setParameters, setEmailAdressAsync, requestQrCodeAsync, setPhoneNumberAsync,
                checkCodeAsync, resendCodeAsync, checkEmailCodeAsync, checkPasswordAsync);
        }

        private async void OnStateUpdated(AuthorizationState? old_state, AuthorizationState new_state)
        {
            await CaptureConsole();

            IsAuthorized = false;
            IsLoggingOut = false;

            switch (new_state)
            {
                case AuthorizationState.AuthorizationStateWaitTdlibParameters:
                    await setParameters();
                    break;
                case AuthorizationState.AuthorizationStateWaitCode:
                    await EnterCodeAsync(old_state, new_state);
                    break;
                case AuthorizationState.AuthorizationStateWaitEmailAddress:                                         //add googleid/appleid ??
                    await EnterAuthentificationInfoAsync(old_state, new_state, "email", async delegate ()
                    {
                        string? email = await Read();
                        return await setEmailAdressAsync(email);
                    });
                    break;
                case AuthorizationState.AuthorizationStateWaitPhoneNumber:
                    Console.WriteLine("Authorize via QR Code?[Y]");
                    if (Console.ReadLine() == "Y")
                    {
                        await requestQrCodeAsync();
                        break;
                    }
                    await EnterAuthentificationInfoAsync(old_state, new_state, "phone number", async delegate ()
                    {
                        string phone_number = await Read();
                        PhoneNumberAuthenticationSettings settings = new()
                        {
                            IsCurrentPhoneNumber = false,
                            AllowFlashCall = false,
                            AllowSmsRetrieverApi = false
                        };
                        return await setPhoneNumberAsync(phone_number, settings);
                    });
                    break;
                case AuthorizationState.AuthorizationStateWaitPassword:
                    await EnterAuthentificationInfoAsync(old_state, new_state, "password", async delegate ()
                    {
                        string hint = ((AuthorizationState.AuthorizationStateWaitPassword)new_state).PasswordHint;
                        if (hint.Length != 0)
                        {
                            await Write($"Password hint: {hint}");
                        }
                        string password = await Read();
                        return await checkPasswordAsync(password);
                    });
                    break;
                case AuthorizationState.AuthorizationStateWaitEmailCode:                                            //add googleid/appleid ??
                    await EnterAuthentificationInfoAsync(old_state, new_state, "email code", async delegate ()
                    {
                        var pattern = ((AuthorizationState.AuthorizationStateWaitEmailCode)new_state).CodeInfo.EmailAddressPattern;
                        await Write(pattern);
                        DateTime valid_until = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                        valid_until = valid_until.AddSeconds(((AuthorizationState.AuthorizationStateWaitEmailCode)new_state).NextPhoneNumberAuthorizationDate).ToLocalTime();
                        TimeSpan valid_for = valid_until.Subtract(DateTime.UtcNow);
                        await Write($"Email will be valid for the next {valid_for.Seconds} seconds");

                        var email_code = new EmailAddressAuthentication.EmailAddressAuthenticationCode
                        {
                            Code = await Read()
                        };
                        return await checkEmailCodeAsync(email_code);

                    });
                    break;
                case AuthorizationState.AuthorizationStateWaitOtherDeviceConfirmation:
                    await Write("Open Telegram on your phone -> go to Settings -> Link Desktop Device -> Scan this QR:");
                    await Write(((AuthorizationState.AuthorizationStateWaitOtherDeviceConfirmation)new_state).Link);
                    break;
                case AuthorizationState.AuthorizationStateWaitRegistration:
                    await Write("Phone number not registered. Use antoher client to do so.");
                    throw new NotImplementedException("User not registred");
                case AuthorizationState.AuthorizationStateReady:
                    IsAuthorized = true;
                    await Write("Authorization successfully completed.");
                    FreeConsole();
                    break;
                case AuthorizationState.AuthorizationStateLoggingOut:
                    IsLoggingOut = true;
                    await Write("Logging Out...");
                    break;
                case AuthorizationState.AuthorizationStateClosing:
                    IsLoggingOut = true;
                    await Write("Closing the application(wait for saving all cats images).");
                    break;
                case AuthorizationState.AuthorizationStateClosed:
                    FreeConsole();
                    await Write("Closed");
                    break;
            }

            return;
        }

        private delegate Task<Ok> SendAuthentificationInfoAsync();
        private async Task<Ok> EnterAuthentificationInfoAsync(AuthorizationState? old_state, AuthorizationState new_state, string state_name, SendAuthentificationInfoAsync send)
        {
            if (old_state == new_state)
            {
                await Write($"Incorrect {state_name}. Enter {state_name} again:");
            }
            else
            {
                await Write($"Enter {state_name}:");
            }

            return await send();
        }

        private async Task EnterCodeAsync(AuthorizationState? old_state, AuthorizationState new_state)
        {
            string? code;
            AuthenticationCodeInfo info = ((AuthorizationState.AuthorizationStateWaitCode)new_state).CodeInfo;
            AuthenticationCodeType codeType = info.Type;

            if (old_state != new_state || resend_flag)
            {

                switch (codeType)
                {
                    case AuthenticationCodeType.AuthenticationCodeTypeCall:
                        await Write($"We have send you code via phone call to {info.PhoneNumber}.");
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeMissedCall:
                        await Write($"We have send you code by missed call to {info.PhoneNumber}. Enter last ? digits:");               //how much digits needed?
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeFragment:
                        await Write($"We have send you code to your NFT phone number. Check https://fragment.com:");
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeSms:
                        await Write($"We have send you code by sms to {info.PhoneNumber}:");
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeTelegramMessage:
                        await Write($"We have send you code via telegram message. Check your any other logged in device:");
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeFlashCall:
                        throw new InvalidOperationException("Flash call code authentification type.");
                }

                await Write($"Code will be valid for the next {info.Timeout} seconds | ({DateTime.Now})");
                await Write("Enter authentification code or empty string to to get a new one: ");
            }
            else
            {
                await Write("Incorrect code. Try it again or enter empty string to to get a new one: ");
            }

            code = Console.ReadLine();
            if (code == null || code.Length == 0)
            {
                await resendCodeAsync();
                resend_flag = true;
            }
            else
            {
                await checkCodeAsync(code);
                resend_flag = false;
            }
        }

        internal override void Panic()
        {

        }


    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TdLib.TdApi;
using TdLib;
using TelegramShellClient;

namespace TelegramShellClient
{
    internal class Authorization
    {

        private static Authorization? instance = null;
        private readonly TdClient? _client = null;
        private readonly Updater updater;

        private Authorization(TdClient client)
        {
            isLoggingOut = false;
            isAuthorized = false;
            _client = client;
            updater = Updater.getInstance(client); 
            if (!updater.TryRegistrateHandler(StateUpdateHandler, new Update.UpdateAuthorizationState()))
            {
                throw new UnauthorizedAccessException("Error registrating update handler. UpdateAuthorization state already captured. Cant initialize Authorization class");
            }
        }

        public static Authorization getInstance(TdClient client)
        {
            return instance == null ? new Authorization(client) : instance;
        }

        private AuthorizationState? _old_state = null;
        public bool isAuthorized { get; private set; }
        public bool isLoggingOut { get; private set; }

        private bool resend_flag = false;

        private async Task StateUpdateHandler(Update update)
        {
            AuthorizationState? new_state = ((Update.UpdateAuthorizationState)update).AuthorizationState;

            isAuthorized = false;
            isLoggingOut = false;

            switch (new_state)
            {
                case AuthorizationState.AuthorizationStateWaitTdlibParameters:
                    await Application.SetParametrsAsync();
                    break;
                case AuthorizationState.AuthorizationStateWaitCode:
                    await EnterCodeAsync(new_state);
                    break;
                case AuthorizationState.AuthorizationStateWaitEmailAddress:                                         //add googleid/appleid ??
                    await EnterAuthentificationInfoAsync(new_state, "email", async delegate ()
                    {
                        string? email = Console.ReadLine();
                        return await _client.SetAuthenticationEmailAddressAsync(email);
                    });
                    break;
                case AuthorizationState.AuthorizationStateWaitPhoneNumber:
                    Console.WriteLine("Authorize via QR Code?[Y]");
                    if (Console.ReadLine() == "Y")
                    {
                        await _client.RequestQrCodeAuthenticationAsync();
                        break;
                    }
                    await EnterAuthentificationInfoAsync(new_state, "phone number", async delegate ()
                    {
                        string? phone_number = Console.ReadLine();
                        PhoneNumberAuthenticationSettings settings = new PhoneNumberAuthenticationSettings();
                        settings.IsCurrentPhoneNumber = false;
                        settings.AllowFlashCall = false;
                        settings.AllowSmsRetrieverApi = false;
                        return await _client.SetAuthenticationPhoneNumberAsync(phone_number, settings);
                    });
                    break;
                case AuthorizationState.AuthorizationStateWaitPassword:
                    await EnterAuthentificationInfoAsync(new_state, "password", async delegate ()
                    {
                        string hint = ((AuthorizationState.AuthorizationStateWaitPassword)new_state).PasswordHint;
                        if (hint.Length != 0)
                        {
                            Console.WriteLine($"Password hint: {hint}");
                        }
                        string? password = Console.ReadLine();
                        return await _client.CheckAuthenticationPasswordAsync(password);
                    });
                    break;
                case AuthorizationState.AuthorizationStateWaitEmailCode:                                            //add googleid/appleid ??
                    await EnterAuthentificationInfoAsync(new_state, "email code", async delegate ()
                    {
                        var pattern = ((AuthorizationState.AuthorizationStateWaitEmailCode)new_state).CodeInfo.EmailAddressPattern;
                        Console.WriteLine(pattern);

                        DateTime valid_until = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                        valid_until = valid_until.AddSeconds(((AuthorizationState.AuthorizationStateWaitEmailCode)new_state).NextPhoneNumberAuthorizationDate).ToLocalTime();
                        TimeSpan valid_for = valid_until.Subtract(DateTime.UtcNow);
                        Console.WriteLine($"Email will be valid for the next {valid_for.Seconds} seconds");

                        var email_code = new EmailAddressAuthentication.EmailAddressAuthenticationCode();
                        email_code.Code = Console.ReadLine();
                        return await _client.CheckAuthenticationEmailCodeAsync(email_code);

                    });
                    break;
                case AuthorizationState.AuthorizationStateWaitOtherDeviceConfirmation:
                    Console.WriteLine("Open Telegram on your phone -> go to Settings -> Link Desktop Device -> Scan this QR:");
                    Console.WriteLine(((AuthorizationState.AuthorizationStateWaitOtherDeviceConfirmation)new_state).Link);
                    break;
                case AuthorizationState.AuthorizationStateWaitRegistration:
                    Console.WriteLine("Phone number not registered. Use antoher client to do so.");
                    throw new NotImplementedException("User not registred");
                case AuthorizationState.AuthorizationStateReady:
                    isAuthorized = true;
                    Console.WriteLine("Authorization successfully completed.");
                    break;
                case AuthorizationState.AuthorizationStateLoggingOut:
                    isLoggingOut = true;
                    Console.WriteLine("Logging Out...");
                    break;
                case AuthorizationState.AuthorizationStateClosing:
                    isLoggingOut = true;
                    Console.WriteLine("Closing the application(wait for saving all cats images).");
                    break;
                case AuthorizationState.AuthorizationStateClosed:
                    Console.WriteLine("Closed");
                    break;
            }

            _old_state = new_state;
            return;
        }

        private delegate Task<Ok> SendAuthentificationInfoAsync();

        private async Task<Ok> EnterAuthentificationInfoAsync(AuthorizationState new_state, string state_name, SendAuthentificationInfoAsync send)
        {
            if (_old_state == new_state)
            {
                Console.WriteLine($"Incorrect {state_name}. Enter {state_name} again:");
            }
            else
            {
                Console.WriteLine($"Enter {state_name}:");
            }

            return await send();
        }

        private async Task EnterCodeAsync(AuthorizationState new_state)
        {
            string? code;
            AuthenticationCodeInfo info = ((AuthorizationState.AuthorizationStateWaitCode)new_state).CodeInfo;
            AuthenticationCodeType codeType = info.Type;

            if (_old_state != new_state || resend_flag)
            {

                switch (codeType)
                {
                    case AuthenticationCodeType.AuthenticationCodeTypeCall:
                        Console.WriteLine($"We have send you code via phone call to {info.PhoneNumber}.");
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeMissedCall:
                        Console.WriteLine($"We have send you code by missed call to {info.PhoneNumber}. Enter last 6 digits:");
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeFragment:
                        Console.WriteLine($"We have send you code to your NFT phone number. Check https://fragment.com:");
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeSms:
                        Console.WriteLine($"We have send you code by sms to {info.PhoneNumber}:");
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeTelegramMessage:
                        Console.WriteLine($"We have send you code via telegram message. Check your any other logged in device:");
                        break;
                    case AuthenticationCodeType.AuthenticationCodeTypeFlashCall:
                        throw new InvalidOperationException("Flash call code authentification type.");
                }

                Console.WriteLine($"Code will be valid for the next {info.Timeout} seconds | ({DateTime.Now.ToString()})");
                Console.WriteLine("Enter authentification code or empty string to to get a new one: ");
            }
            else
            {
                Console.WriteLine("Incorrect code. Try it again or enter empty string to to get a new one: ");
            }

            code = Console.ReadLine();
            if (code == null || code.Length == 0)
            {
                await _client.ResendAuthenticationCodeAsync();
                resend_flag = true;
            }
            else
            {
                await _client.CheckAuthenticationCodeAsync(code);
                resend_flag = false;
            }
        }

        private async Task LogOutAsync()
        {
            await _client.LogOutAsync();
        }
    }

}

using Newtonsoft.Json.Bson;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using TdLib;
using TdLib.Bindings;
using static TdLib.TdApi;


namespace TelegramShellClient
{
    internal static partial class Application
    {
        private const string LogFile = "TSC.txt";
        private const string DataDir = "Data";
        private const int APP_ID = 0;
        private const string API_HASH = "";
        private const string version = "0.1";
        private static TdClient _client = new TdClient();
        private static Authorization _authorization = Authorization.GetInstance
            (
                SetParametrsAsync,
                delegate (string email)
                { return _client.ExecuteAsync<Ok>(new SetAuthenticationEmailAddress() { EmailAddress = email }); },
                delegate ()
                { return _client.ExecuteAsync<Ok>(new RequestQrCodeAuthentication()); },
                delegate (string phone, PhoneNumberAuthenticationSettings settings)
                { return _client.ExecuteAsync<Ok>(new SetAuthenticationPhoneNumber() { PhoneNumber = phone, Settings = settings }); },
                delegate (string code)
                { return _client.ExecuteAsync<Ok>(new CheckAuthenticationCode() { Code = code }); },
                delegate ()
                { return _client.ExecuteAsync<Ok>(new ResendAuthenticationCode()); },
                delegate (EmailAddressAuthentication.EmailAddressAuthenticationCode code)
                { return _client.ExecuteAsync<Ok>(new CheckAuthenticationEmailCode() { Code = code }); },
                delegate (string password)
                { return _client.ExecuteAsync<Ok>(new CheckAuthenticationPassword() { Password = password }); }
            );

        static Application()
        {
            _client.Bindings.SetLogVerbosityLevel(0);
            _client.Bindings.SetLogFilePath(LogFile);
            _client.SetLogStreamAsync(null);
        }


        private static Task<Ok> SetParametrsAsync()
        {
            return _client.SetTdlibParametersAsync(false, DataDir, DataDir, null, true, true, true, true, APP_ID, API_HASH,
                "en", Environment.MachineName, Environment.OSVersion.VersionString, version, true, false);
        }

        public static void Main(string[] args)
        {

        }
    }
}
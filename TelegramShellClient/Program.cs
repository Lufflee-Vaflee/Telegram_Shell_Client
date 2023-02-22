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
        private static Authorization _authorization = Authorization.getInstance(_client);

        static Application()
        {
            _client.Bindings.SetLogVerbosityLevel(0);
            _client.Bindings.SetLogFilePath(LogFile);
            _client.SetLogStreamAsync(null);
            var d = new newAuthorization(0, executeAsync<Ok>, executeAsync<Ok>);
        }

        static private async Task<TResult> executeAsync<TResult>(Function<TResult> function) where TResult : TdApi.Object
        {
            return await _client.ExecuteAsync<TResult>(function);
        }

        public static async Task SetParametrsAsync()
        {
            await _client.SetTdlibParametersAsync(false, DataDir, DataDir, null, true, true, true, true, APP_ID, API_HASH,
                "en", Environment.MachineName, Environment.OSVersion.VersionString, version, true, false);
        }

        public static void Main(string[] args)
        {

        }
    }
}
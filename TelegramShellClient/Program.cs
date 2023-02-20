using Newtonsoft.Json.Bson;
using Spectre.Console;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using TdLib;
using TdLib.Bindings;
using static TdLib.TdApi;


namespace TelegramShellClient
{
    public class Application
    {
        private const string LogFile = "TSC.txt";
        private const string DataDir = "Data";
        private const int APP_ID = 0;
        private const string API_HASH = "";
        private const string version = "0.1";
        private static TdLib.TdClient _client = new TdLib.TdClient();
        private static TelegramShellClient.Authorization _authorization = Authorization.getInstance(_client);

        static Application()
        {
            _client.Bindings.SetLogVerbosityLevel(0);
            _client.Bindings.SetLogFilePath(LogFile);
            _client.SetLogStreamAsync(null);
        }

        public static async Task SetParametrsAsync()
        {
            await _client.SetTdlibParametersAsync(false, DataDir, DataDir, null, true, true, true, true, APP_ID, API_HASH,
                "en", Environment.MachineName, Environment.OSVersion.VersionString, version, true, false);
        }

        private static class ChatListsManager
        {
            private static List<ChatFilterInfo>? filters = null;

            static ChatListsManager()
            {
                /*Update.UpdateChatFilters a;
                ChatFilterInfo b = a.ChatFilters[0];
                
                ChatLists c;
                var d = c.ChatLists_[0];
                ChatList.ChatListMain e;
                ChatList.ChatListFilter f;
                f.ChatFilterId;
                ChatList.ChatListArchive g;
                ChatFilter asd;

                _client.AddChatToListAsync();
                _client.CreateChatFilterAsync();
                _client.GetChatListsToAddChatAsync();
                _client.DeleteChatFilterAsync();
                _client.EditChatFilterAsync();
                _client.GetChatFilterAsync();
                _client.GetChatFilterDefaultIconNameAsync();
                _client.GetRecommendedChatFiltersAsync();
                _client.ReorderChatFiltersAsync();
                _client.UpdateReceived += updateHandler;
                */
            }

            private static void updateHandler(object? sender, Update update)
            {
                if (update is not Update.UpdateChatFilters || sender == null || !sender.Equals(_client))
                {
                    return;
                }

                filters = ((Update.UpdateChatFilters)update).ChatFilters.ToList();
            }


        }

        public static void Main(string[] args)
        {
        }
    }
}
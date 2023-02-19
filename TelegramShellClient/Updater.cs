using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TdLib;

namespace TelegramShellClient
{
    //делегирует обработку события обновления в зависимости от его типа
    //следит за исключетильным доступом к обработке конкретного типа обновления
    internal class Updater
    {
        private readonly TdClient? _client = null;
        private readonly Dictionary<string, Handler> handlers = new Dictionary<string, Handler>();
        private static Updater? Instance = null;

        private Updater(TdClient client)
        {
            _client = client;
            _client.UpdateReceived += UpdateHandler;
        }

        public static Updater getInstance(TdClient client)
        {
            return Instance == null ? new Updater(client) : Instance;
        }

        private async void UpdateHandler(object? sender, TdApi.Update update)
        {
            if (sender == null || !sender.Equals(_client))
            {
                return;
            }

            Handler? handler;
            if (handlers.TryGetValue(update.DataType, out handler))
            {
                await handler(update);
            }
            else
            {
                //Console.WriteLine($"Unregistered update: {update}");
            }
        }

        public delegate Task Handler(TdApi.Update update);

        public bool TryRegistrateHandler(Handler handler, TdApi.Update type)
        {
            try
            {
                handlers.Add(type.DataType, handler);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

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
    internal static partial class Application
    {
        internal static class Updater
        {
            private static readonly Dictionary<string, Handler> handlers = new Dictionary<string, Handler>();

            static Updater()
            {
                _client.UpdateReceived += UpdateHandler;
            }


            static private void UpdateHandler(object? sender, TdApi.Update update)
            {
                if (sender == null || !sender.Equals(_client))
                {
                    return;
                }

                Handler? handler;
                if (handlers.TryGetValue(update.DataType, out handler))
                {
                    handler(update);
                }
                else
                {
                    //Console.WriteLine($"Unregistered update: {update}");
                }
            }

            public delegate void Handler(TdApi.Update update);

            static public bool TryRegistrateHandler(Handler handler, TdApi.Update type)
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
}

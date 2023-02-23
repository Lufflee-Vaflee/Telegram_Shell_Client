using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdLib;

namespace TelegramShellClient
{
    internal class Updatable<Type, Update> where Update : TdApi.Update
    {

        private Type? _state;
        public Type? State
        {
            get
            {
                lock(updating)
                {
                    return _state;
                }
            }
            private set
            {
                _state = value;
            }
        }


        private readonly object updating = new();

        public delegate Type unpackState(Update update);

        private readonly unpackState unpack;

        public Updatable(unpackState unpack)
        {
            this.unpack = unpack;
            if(!Application.Updater.TryRegistrateHandler<Update>(UpdateHandler))
            {
                throw new UnauthorizedAccessException($"Error registrating update handler. {default(Update).DataType} is already captured");
            }
        }

        public delegate void Updated(Type? oldState, Type newState);

        public event Updated? Notify;

        private void UpdateHandler(TdApi.Update update)
        {
            Type? old_state;
            lock (updating)
            {
                old_state = _state;
                State = unpack((Update)update);
            }

            Notify?.BeginInvoke(old_state, State, null, "state Updated");
        }
    }
}

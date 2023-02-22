using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdLib;

namespace TelegramShellClient
{
    internal class Updatable<State>
    {

        private State? _state;
        public State? state
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


        private object updating = new object();

        public delegate State unpackState(TdApi.Update update);

        private unpackState unpack;

        public Updatable(TdApi.Update update, unpackState unpack)
        {
            this.unpack = unpack;
            if(!Application.Updater.TryRegistrateHandler(UpdateHandler, update))
            {
                throw new UnauthorizedAccessException($"Error registrating update handler. {update.DataType} is already captured");
            }
        }

        delegate void Updated(State? oldState, State newState);

        event Updated? Notify;

        private void UpdateHandler(TdApi.Update update)
        {
            State? old_state;
            lock (updating)
            {
                old_state = _state;
                state = unpack(update);
            }

            Notify?.BeginInvoke(old_state, state, null, "hello there");
        }
    }
}

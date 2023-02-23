using ReadLineReboot;
using static TelegramShellClient.DialogMediator;

namespace TelegramShellClient
{
    internal abstract class Dialog
    {
        public Dialog(int priority)
        {
            Priority = priority;
        }

        List<string>? cache = null;

        public int Priority { get; private set; }

        public bool IsConsoleOwner { get; private set; } = false;

        public void OnCaptureLost()
        {
            cache = ReadLine.GetHistory();
            IsConsoleOwner = false;
            ReadLine.InterruptRead();
            Panic();
        }

        internal abstract void Panic();

        internal async Task CaptureConsole()
        {
            if (!IsConsoleOwner)
            {
                await DialogMediator.Capture(this);
                IsConsoleOwner = true;
                ReadLine.ClearHistory();
                RestoreHistory();
            }
        }

        private void RestoreHistory()
        {
            if (cache != null)
            {
                foreach (string Line in cache)
                {
                    TryWrite(Line);
                }
            }
        }

        internal bool FreeConsole()
        {
            IsConsoleOwner = false;
            cache?.Clear();
            return tryFree(this);
        }

        internal bool TryWrite(in string Line)
        {
            return tryWriteLine(this, Line);
        }

        internal async Task Write(string line)
        {
            while(!TryWrite(line))
            {
                await CaptureConsole();
            }
        }

        internal bool TryRead(out string? line, in string prompt = "", in string default_text = "")
        {
            try
            {
                return TryReadLine(this, out line, prompt, default_text);
            }
            catch
            {
                line = null;
                return false;
            }
        }

        internal async Task<string> Read(string prompt = "", string default_text = "")
        {
            string? result;
            while (!TryRead(out result, prompt, default_text))
            {
                await CaptureConsole();
            }
            result ??= string.Empty;

            return result;
        }

        internal bool TryInteract<T>(out T? result, consoleInteraction<T> interaction)
        {
            return tryInteract<T>(this, out result, interaction);
        }

        internal async Task<T?> Interact<T>(consoleInteraction<T> interaction)
        {
            T? result;
            while(!TryInteract<T>(out result, interaction))
            {
                await CaptureConsole();
            }
            return result;
        }

        internal bool TryInteract(consoleInteraction interaction)
        {
            return DialogMediator.tryInteract(this, interaction);
        }

        internal async Task Interact(consoleInteraction interaction)
        {
            while (!TryInteract(interaction))
            {
                await CaptureConsole();
            }
        }
    }
}

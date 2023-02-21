using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReadLineReboot;
using ReadLineReboot.Abstractions;
using static TelegramShellClient.DialogMediator;

namespace TelegramShellClient
{
    internal abstract class Dialog
    {
        public Dialog(int priority)
        {
            _priority = priority;
        }

        List<string>? cache = null;

        public int _priority { get; private set; }

        public bool isConsoleOwner { get; private set; } = false;

        public void onCaptureLost()
        {
            cache = ReadLine.GetHistory();
            isConsoleOwner = false;
            ReadLine.InterruptRead();
            panic();
        }

        internal abstract void panic();

        private async Task CaptureConsole()
        {
            await DialogMediator.Capture(this);
            isConsoleOwner = true;
            ReadLine.ClearHistory();
            restoreHistory();
        }

        private void restoreHistory()
        {
            if (cache != null)
            {
                foreach (string Line in cache)
                {
                    tryWrite(Line);
                }
            }
        }

        private bool FreeConsole()
        {
            isConsoleOwner = false;
            return DialogMediator.tryFree(this);
        }

        private bool tryWrite(in string Line)
        {
            return DialogMediator.tryWriteLine(this, Line);
        }

        private bool tryRead(out string? line, in string prompt = "", in string default_text = "")
        {
            try
            {
                return DialogMediator.tryReadLine(this, out line, prompt, default_text);
            }
            catch
            {
                line = null;
                return false;
            }
        }

        public bool tryInteract<T>(out T? result, consoleInteraction<T> interaction)
        {
            return DialogMediator.tryInteract<T>(this, out result, interaction);
        }

        public bool tryInteract(consoleInteraction interaction)
        {
            return DialogMediator.tryInteract(this, interaction);
        }
    }
}

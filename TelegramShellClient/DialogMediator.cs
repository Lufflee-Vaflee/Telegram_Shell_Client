using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading;
using TdLib;
using ReadLineReboot;

namespace TelegramShellClient
{
    //Класс отвечающий за предоставление доступа к исполнению любого метода консоли
    //предоставление доступа осуществляется на основе приоритета
    //более высокий приоритет над текущим - захват доступа выполниться принудительно сразу же после завершения текущей операции
    //одинаковый или более низкий приоритет - захват доступа выполниться после освобождения текущим захватчиком
    internal static class DialogMediator
    {
        
        private static readonly object _capture = new object();

        private static readonly object _console = new object();

        private static readonly SemaphoreSlim CaptureAvailible = new SemaphoreSlim(0, 1);

        public static Dialog? currentOwner { get; private set; } = null;

        static DialogMediator()
        {
            ReadLine.Interruptible = true;
            ReadLine.InterruptionResponsiveness = 64;
            ReadLine.HistoryEnabled = true;
            ReadLine.CtrlCEnabled = true;
        }

        public static async Task Capture(Dialog capturing)
        {
            while(!tryCapture(capturing))
            {
                await CaptureAvailible.WaitAsync();
                CaptureAvailible.Release();
            }
        }

        public static bool tryCapture(Dialog capturing)
        {
            lock(_capture)
            {
                if (currentOwner == null)
                {
                    currentOwner = capturing;
                    CaptureAvailible.Wait();
                    return true;
                }
                else if (capturing._priority > currentOwner._priority)
                {
                    currentOwner.onCaptureLost();
                    currentOwner = capturing;
                    Console.Clear();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool tryFree(Dialog supposedOwner)
        {
            lock (_capture)
            {
                if (supposedOwner.Equals(currentOwner))
                {
                    currentOwner = null;
                    CaptureAvailible.Release();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool tryWriteLine(Dialog supposedOwner, in string line)
        {
            lock (_capture)
            {
                if (supposedOwner.Equals(currentOwner))
                {
                    writeLine(line);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool tryReadLine(Dialog supposedOwner, out string? line, in string prompt = "", in string default_text = "")
        {
            lock (_capture)
            {

                if (supposedOwner.Equals(currentOwner))
                {
                    line = readLine();
                    return true;
                }
                else
                {
                    line = null;
                    return false;
                }
            }
        }

        public delegate T consoleInteraction<T>();
        public delegate void consoleInteraction();

        public static bool tryInteract<T>(Dialog supposedOwner, out T? result, consoleInteraction<T> interaction)
        {
            lock (_console)
            {
                if (supposedOwner.Equals(currentOwner))
                {
                    result = interaction();
                    return true;
                }
                else
                {
                    result = default(T);
                    return false;
                }
            }
        }

        public static bool tryInteract(Dialog supposedOwner, consoleInteraction interaction)
        {
            lock (_console)
            {
                if (supposedOwner.Equals(currentOwner))
                {
                    interaction();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private static string? readLine(in string prompt = "", in string default_text = "")
        {
            lock(_console)
            {
                return ReadLine.Read(prompt, default_text);
            }
        }

        private static void writeLine(in string line)
        {
            lock (_console)
            {
                Console.WriteLine(line);
            }
        }
    }
}

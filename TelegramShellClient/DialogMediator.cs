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
        
        private static readonly object _capture = new();

        private static readonly object _console = new();

        private static readonly SemaphoreSlim CaptureAvailible = new(0, 1);

        public static Dialog? CurrentOwner { get; private set; } = null;

        static DialogMediator()
        {
            ReadLineReboot.ReadLine.Interruptible = true;
            ReadLineReboot.ReadLine.InterruptionResponsiveness = 64;
            ReadLineReboot.ReadLine.HistoryEnabled = true;
            ReadLineReboot.ReadLine.CtrlCEnabled = true;
        }

        public static async Task Capture(Dialog capturing)
        {
            while(!TryCapture(capturing))
            {
                await CaptureAvailible.WaitAsync();
                CaptureAvailible.Release();
            }
        }

        public static bool TryCapture(Dialog capturing)
        {
            lock(_capture)
            {
                if (CurrentOwner == null)
                {
                    CurrentOwner = capturing;
                    CaptureAvailible.Wait();
                    return true;
                }
                else if (capturing.Priority > CurrentOwner.Priority)
                {
                    CurrentOwner.OnCaptureLost();
                    CurrentOwner = capturing;
                    Console.Clear();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool TryFree(Dialog supposedOwner)
        {
            lock (_capture)
            {
                if (supposedOwner.Equals(CurrentOwner))
                {
                    CurrentOwner = null;
                    CaptureAvailible.Release();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool TryWriteLine(Dialog supposedOwner, in string line)
        {
            lock (_capture)
            {
                if (supposedOwner.Equals(CurrentOwner))
                {
                    WriteLine(line);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool TryReadLine(Dialog supposedOwner, out string? line, in string prompt = "", in string default_text = "")
        {
            lock (_capture)
            {

                if (supposedOwner.Equals(CurrentOwner))
                {
                    line = ReadLine(prompt, default_text);
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

        public static bool TryInteract<T>(Dialog supposedOwner, out T? result, consoleInteraction<T> interaction)
        {
            lock (_console)
            {
                if (supposedOwner.Equals(CurrentOwner))
                {
                    result = interaction();
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }
        }

        public static bool TryInteract(Dialog supposedOwner, consoleInteraction interaction)
        {
            lock (_console)
            {
                if (supposedOwner.Equals(CurrentOwner))
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

        private static string? ReadLine(in string prompt = "", in string default_text = "")
        {
            lock(_console)
            {
                return ReadLineReboot.ReadLine.Read(prompt, default_text);
            }
        }

        private static void WriteLine(in string line)
        {
            lock (_console)
            {
                Console.WriteLine(line);
            }
        }
    }
}



using Spectre.Console;
using System.Collections.Concurrent;

namespace TelegramShellClient
{
    //Класс отвечающий за предоставление доступа к исполнению любой функции содержащий ввод/вывод
    //предоставление доступа осуществляется на основе приоритета
    //более высокий приоритет над текущим - захват доступа выполниться принудительно сразу же после завершения текущей операции
    //одинаковый или более низкий приоритет - захват доступа выполниться после освобождения текущим захватчиком
    internal static class DialogMediator
    {
        
        private static readonly object сaptureRequest = new object();

        private static readonly SemaphoreSlim CaptureAvailible = new SemaphoreSlim(0, 1);

        public static Dialog? currentOwner { get; private set; } = null;

        static public bool tryCapture(Dialog capturing)
        {
            lock (сaptureRequest)
            {
                if (currentOwner == null)
                {
                    CaptureAvailible.Wait();
                    currentOwner = capturing;
                    return true;
                }
                else if (capturing._priority > currentOwner._priority)
                {
                    currentOwner.onCaptureLost();
                    tryFree(currentOwner);
                    CaptureAvailible.Wait();
                    currentOwner = capturing;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        static public async Task Capture(Dialog capturing)
        {
            while (!tryCapture(capturing))
            {
                await CaptureAvailible.WaitAsync();
                CaptureAvailible.Release();
            }
        }

        static public bool tryFree(Dialog owner)
        {
            lock (сaptureRequest)
            {
                if (owner.Equals(currentOwner))
                {
                    currentOwner = null;
                    CaptureAvailible.Release();
                    return true;
                }
                return false;
            }
        }

        static public bool tryRead(Dialog requester, out string? input)
        {
            lock (сaptureRequest)
            {
                input = null;
                if (requester.Equals(currentOwner))
                {
                    input = Console.ReadLine();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        static public bool tryWrite(Dialog requester, in string output)
        {
            lock (сaptureRequest)
            {
                if (requester.Equals(currentOwner))
                {
                    Console.WriteLine(output);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}

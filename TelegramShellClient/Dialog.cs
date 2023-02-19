using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramShellClient
{
    internal class Dialog
    {

        public int _priority { get; private set; }

        public void onCaptureLost()
        {
            int a = 2;
        }


        public Dialog(int priority)
        {
            _priority = priority;
        }

        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet
{
    public class DebugConsoleMessageEventArgs : EventArgs
    {
        public ConsoleColor Color { get; private set; }

        public string Message { get; private set; }

        public DebugConsoleMessageEventArgs(string message, ConsoleColor color)
        {
            Message = message;
            Color = color;
        }
    }
}

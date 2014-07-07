using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet
{
    public interface IDebugConsoleController
    {
        /// <summary>
        /// Raised when the source needs to log a message.
        /// </summary>
        event EventHandler<DebugConsoleMessageEventArgs> OnMessage;

        void Log(string message, ConsoleColor color);
    }
}

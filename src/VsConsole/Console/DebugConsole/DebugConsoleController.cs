using NuGet;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetConsole
{
    [Export(typeof(IDebugConsoleController))]
    public class DebugConsoleController : IDebugConsoleController
    {
        public DebugConsoleController()
        {

        }

        public void Log(string message)
        {
            Log(message, ConsoleColor.White);
        }

        public void Log(string message, ConsoleColor color)
        {
            Log(message, color, null, null, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "bytes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "span")]
        public void Log(string message, ConsoleColor color, TimeSpan? span, int? bytes, Guid? context)
        {
            if (OnMessage != null)
            {
                DebugConsoleMessageEventArgs args = new DebugConsoleMessageEventArgs(message, color);

                OnMessage(this, args);
            }
        }

        public event EventHandler<DebugConsoleMessageEventArgs> OnMessage;
    }
}

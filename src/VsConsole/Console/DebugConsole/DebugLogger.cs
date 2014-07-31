using NuGet;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NuGetConsole.Implementation
{
    [Export(typeof(IDebugLogger))]
    public class DebugLogger : IDebugLogger
    {
        private DebugConsoleToolWindow _console;

        public DebugLogger()
        {

        }

        public void Log(string message, ConsoleColor color)
        {
            if (_console != null)
            {
                _console.Log(message, color);
            }
        }

        public void SetConsole(DebugConsoleToolWindow console)
        {
            _console = console;
        }
    }
}

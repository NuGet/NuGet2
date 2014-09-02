using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio
{
    [Export(typeof(ILogger))]
    public class VsLogger : ILogger
    {
        private IDebugConsoleController _debugConsole;
        
        [ImportingConstructor]
        public VsLogger(IDebugConsoleController debugConsole)
        {
            _debugConsole = debugConsole;
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            _debugConsole.Log(
                String.Format(CultureInfo.CurrentCulture, message, args),
                GetColor(level));
        }

        private ConsoleColor GetColor(MessageLevel level)
        {
            switch (level)
            {
            case MessageLevel.Info:
                return ConsoleColor.Green;
            case MessageLevel.Warning:
                return ConsoleColor.Yellow;
            case MessageLevel.Debug:
                return ConsoleColor.Cyan;
            case MessageLevel.Error:
                return ConsoleColor.Red;
            default:
                return ConsoleColor.White;
            }
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            // We should never be called to do this. So throw and Debug.Fail if we do
            Debug.Fail("Never expected to be called!");
            throw new NotImplementedException();
        }
    }
}

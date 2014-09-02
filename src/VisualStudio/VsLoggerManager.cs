using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio
{
    public interface ILoggerManager
    {
        ILogger GetLogger(string name);
    }

    [Export(typeof(ILoggerManager))]
    public class VsLoggerManager : ILoggerManager
    {
        private ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string,ILogger>(StringComparer.OrdinalIgnoreCase);
        private ILogger _globalLogger;
        
        [ImportingConstructor]
        public VsLoggerManager(IDebugConsoleController debugConsole)
        {
            _globalLogger = new DebugConsoleLogger(debugConsole);
        }

        public ILogger GetLogger(string name)
        {
            return _loggers.GetOrAdd(name, n => new NamedLogger(_globalLogger, n));
        }
    }

    internal class DebugConsoleLogger : ILogger
    {
        private IDebugConsoleController _debugConsole;

        public DebugConsoleLogger(IDebugConsoleController debugConsole)
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

    internal class NamedLogger : ILogger
    {
        private ILogger _output;
        public string Name { get; private set; }

        public NamedLogger(ILogger output, string name)
        {
            Name = name;
            _output = output;
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            string baseMessage = String.Format(CultureInfo.CurrentCulture, message, args);
            _output.Log(
                level,
                String.Format(CultureInfo.CurrentCulture, "[{0}] {1}", Name, baseMessage));
                    
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            return _output.ResolveFileConflict(message);
        }
    }
}

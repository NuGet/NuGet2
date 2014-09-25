using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace NuGet.Client.VisualStudio
{
    public class DebugConsoleTraceListener : TraceListener
    {
        private static readonly Dictionary<TraceEventType, ConsoleColor> _colorMap = new Dictionary<TraceEventType, ConsoleColor>()
        {
            { TraceEventType.Verbose, ConsoleColor.Gray },
            { TraceEventType.Information, ConsoleColor.Green },
            { TraceEventType.Warning, ConsoleColor.Yellow },
            { TraceEventType.Error, ConsoleColor.Red }
        };

        private IDebugConsoleController _console;

        public DebugConsoleTraceListener(IDebugConsoleController console)
        {
            _console = console;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            // Marshal the invocation over to the UI thread
            ThreadHelper.Generic.InvokeAsync(() => TraceEventOnUIThread(
                eventCache,
                source,
                eventType,
                id,
                message));
        }

        public override void Write(string message)
        {
            ThreadHelper.Generic.InvokeAsync(() => _console.Log(DateTime.Now, message, TraceEventType.Verbose, String.Empty));
        }

        public override void WriteLine(string message)
        {
            ThreadHelper.Generic.InvokeAsync(() => _console.Log(DateTime.Now, message, TraceEventType.Verbose, String.Empty));
        }

        private void TraceEventOnUIThread(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            ConsoleColor color;
            if (!_colorMap.TryGetValue(eventType, out color))
            {
                color = ConsoleColor.White;
            }
            _console.Log(eventCache.DateTime, message, eventType, source);
        }
    }
}

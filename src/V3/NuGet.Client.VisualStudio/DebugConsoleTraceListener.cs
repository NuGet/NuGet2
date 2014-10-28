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

        private readonly ThreadHelper _helper;
        private readonly IDebugConsoleController _console;

        public DebugConsoleTraceListener(IDebugConsoleController console, ThreadHelper helper)
        {
            _helper = helper;
            _console = console;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            _helper.InvokeAsync(() =>
            {
                ConsoleColor color;
                if (!_colorMap.TryGetValue(eventType, out color))
                {
                    color = ConsoleColor.White;
                }
                _console.Log(eventCache.DateTime, message, eventType, source);
            });
        }

        public override void Write(string message)
        {
            _helper.InvokeAsync(() =>
                _console.Log(DateTime.Now, message, TraceEventType.Verbose, String.Empty));
        }

        public override void WriteLine(string message)
        {
            _helper.InvokeAsync(() =>
                _console.Log(DateTime.Now, message, TraceEventType.Verbose, String.Empty));
        }
    }
}

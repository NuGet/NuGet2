using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
            ConsoleColor color;
            if (!_colorMap.TryGetValue(eventType, out color))
            {
                color = ConsoleColor.White;
            }
            _console.Log("(" + eventCache.DateTime.ToString("O") + ")[" + Shorten(source) + "]" + message, color);
        }

        public override void Write(string message)
        {
            _console.Log(message, ConsoleColor.White);
        }

        public override void WriteLine(string message)
        {
            _console.Log(message, ConsoleColor.White);
        }

        // Shortens NuGet. trace source names.
        private string Shorten(string source)
        {
            if (source.StartsWith("NuGet."))
            {
                return source.Split('.').Last();
            }
            return source;
        }
    }
}

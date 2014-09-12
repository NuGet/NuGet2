using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;

namespace NuGet
{
    public static class TraceSourceExtensions
    {
        private static string FormatMessage(string eventName, string format, params object[] args)
        {
            return "[" + eventName + "] " + 
                String.Format(CultureInfo.CurrentCulture, format, args);
        }

        public static void Info(this TraceSource self, string eventName, string format, params object[] args)
        {
            self.TraceEvent(
                TraceEventType.Information,
                NuGetEventIds.Unspecified,
                FormatMessage(eventName, format, args));
        }

        public static void Verbose(this TraceSource self, string eventName, string format, params object[] args)
        {
            self.TraceEvent(
                TraceEventType.Verbose,
                NuGetEventIds.Unspecified,
                FormatMessage(eventName, format, args));
        }

        public static void Warning(this TraceSource self, string eventName, string format, params object[] args)
        {
            self.TraceEvent(
                TraceEventType.Warning,
                NuGetEventIds.Unspecified,
                FormatMessage(eventName, format, args));
        }

        public static void Error(this TraceSource self, string eventName, string format, params object[] args)
        {
            self.TraceEvent(
                TraceEventType.Error,
                NuGetEventIds.Unspecified,
                FormatMessage(eventName, format, args));
        }
    }
}

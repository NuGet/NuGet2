using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public static void EnterMethod(this TraceSource self, [CallerMemberName] string method = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = -1)
        {
            self.Verbose(method, "[{0}] ({1}:{2})", method, file, line);
        }

        public static IDisposable TraceMethod(this TraceSource self, [CallerMemberName] string method = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = -1)
        {
            DateTime enterTime = DateTime.UtcNow;
            self.Verbose(method, "[{0}] ({1}:{2}) Entered @ {3}", method, file, line, enterTime.ToString("O", CultureInfo.CurrentCulture));
            return new DisposableAction(() =>
            {
                var exitTime = DateTime.UtcNow;
                var duration = exitTime - enterTime;
                self.Verbose(method + "_exit", "[{0}] Exited @ {1} (duration {2:0.00}ms)", method, exitTime.ToString("O", CultureInfo.CurrentCulture), duration.TotalMilliseconds);
            });
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

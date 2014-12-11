using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using NuGet.Client.Diagnostics;
using NuGet.Client.V3Shim;

namespace NuGet.Client.VisualStudio
{
    public static class VsNuGetDiagnostics
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The listeners don't need to disposed until the process ends, at which point they will be automatically disposed")]
        public static void Initialize(IDebugConsoleController console)
        {
            var consoleListener = new DebugConsoleTraceListener(console, ThreadHelper.Generic);
            var activityLogListener = new ActivityLogTraceListener();
            foreach (var source in AllSources())
            {
                source.Listeners.Add(consoleListener);
                source.Listeners.Add(activityLogListener);
                source.Switch.Level = SourceLevels.All;
            }
        }

        private static IEnumerable<TraceSource> AllSources()
        {
            return NuGetTraceSources.GetAllSources()
                .Concat(VsNuGetTraceSources.GetAllSources()
                    .Concat(V3InteropTraceSources.GetAllSources()));
        }
    }
}

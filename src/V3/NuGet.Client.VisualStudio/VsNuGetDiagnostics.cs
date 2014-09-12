using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;

namespace NuGet.Client.VisualStudio
{
    public static class VsNuGetDiagnostics
    {
        public static void Initialize(IDebugConsoleController console)
        {
            foreach (var source in AllSources())
            {
                source.Listeners.Add(new DebugConsoleTraceListener(console));
                source.Switch.Level = SourceLevels.All;
            }
        }

        private static IEnumerable<TraceSource> AllSources()
        {
            return Enumerable.Concat(
                NuGetTraceSources.GetAllSources(),
                VsNuGetTraceSources.GetAllSources());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;
using NuGet.Client.V3Shim;

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
            return NuGetTraceSources.GetAllSources()
                .Concat(VsNuGetTraceSources.GetAllSources()
                    .Concat(V3InteropTraceSources.GetAllSources()));
        }
    }
}

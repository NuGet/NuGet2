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
            return NuGetTraceSources.GetAllSources() // NuGet.Client
                .Concat(VsNuGetTraceSources.GetAllSources()) // NuGet.Client.VisualStudio
                .Concat(V3InteropTraceSources.GetAllSources()) // NuGet.Client.V3Interop
                .Concat(NuGet.Diagnostics.NuGetTraceSources.GetAllSources()) // NuGet.Core
                .Concat(NuGet.VisualStudio.Diagnostics.VsNuGetTraceSources.GetAllSources()) // NuGet.VisualStudio
            ;
        }
    }
}

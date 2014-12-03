using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio
{
    public static class VsNuGetTraceSources
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource VsProjectInstallationTarget = new TraceSource(typeof(VsProject).FullName);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource VsPowerShellScriptExecutionFeature = new TraceSource(typeof(VsPowerShellScriptExecutor).FullName);

        /// <summary>
        /// Retrieves a list of all sources defined in this class. Uses reflection, store the result!
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This is expensive")]
        public static IEnumerable<TraceSource> GetAllSources()
        {
            return typeof(VsNuGetTraceSources).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => typeof(TraceSource).IsAssignableFrom(f.FieldType))
                .Select(f => (TraceSource)f.GetValue(null));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio.Diagnostics
{
    public static class VsNuGetTraceSources
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource VsProjectSystem = new TraceSource(typeof(VsProjectSystem).FullName);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource VsPackageManager = new TraceSource(typeof(VsPackageManager).FullName);

        /// <summary>
        /// Retrieves a list of all sources defined in this class. Uses reflection, store the result!
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method uses reflection and the results should be cached.")]
        public static IEnumerable<TraceSource> GetAllSources()
        {
            return typeof(VsNuGetTraceSources).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => typeof(TraceSource).IsAssignableFrom(f.FieldType))
                .Select(f => (TraceSource)f.GetValue(null));
        }
    }
}

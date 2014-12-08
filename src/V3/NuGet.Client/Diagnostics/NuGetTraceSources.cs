using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Installation;
using NuGet.Client.Interop;
using NuGet.Client.Resolution;

namespace NuGet.Client.Diagnostics
{
    public static class NuGetTraceSources
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource ActionResolver = new TraceSource(typeof(ActionResolver).FullName);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource ActionExecutor = new TraceSource(typeof(ActionExecutor).FullName);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource CoreInterop = new TraceSource(typeof(CoreInteropProjectManager).Namespace);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource CoreInteropInstalledPackagesList = new TraceSource(typeof(CoreInteropInstalledPackagesList).FullName);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource V2SourceRepository = new TraceSource(typeof(V2SourceRepository).FullName);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource InstallationTarget = new TraceSource(typeof(InstallationTarget).FullName);       
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable enough :).")]
        public static readonly TraceSource V3SourceRepository = new TraceSource(typeof(V3SourceRepository).FullName);              

        /// <summary>
        /// Retrieves a list of all sources defined in this class. Uses reflection, store the result!
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification="This method uses reflection and the results should be cached.")]
        public static IEnumerable<TraceSource> GetAllSources()
        {
            return Enumerable.Concat(
                typeof(NuGetTraceSources).GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => typeof(TraceSource).IsAssignableFrom(f.FieldType))
                    .Select(f => (TraceSource)f.GetValue(null)),
                NuGet.Data.DataTraceSources.GetAllSources());
        }
    }
}

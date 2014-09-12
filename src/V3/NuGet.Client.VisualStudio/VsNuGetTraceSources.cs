using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio
{
    public static class VsNuGetTraceSources
    {
        public static readonly TraceSource VsProjectInstallationTarget = new TraceSource(typeof(VsProjectInstallationTarget).FullName);
        
        /// <summary>
        /// Retrieves a list of all sources defined in this class. Uses reflection, store the result!
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<TraceSource> GetAllSources()
        {
            return typeof(VsNuGetTraceSources).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => typeof(TraceSource).IsAssignableFrom(f.FieldType))
                .Select(f => (TraceSource)f.GetValue(null));
        }
    }
}

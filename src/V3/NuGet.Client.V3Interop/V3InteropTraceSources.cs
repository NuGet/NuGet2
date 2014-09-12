using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V3Shim
{
    public static class V3InteropTraceSources
    {
        public static readonly TraceSource Dispatcher = new TraceSource(typeof(InterceptDispatcher).FullName);
        public static readonly TraceSource Channel = new TraceSource(typeof(InterceptChannel).FullName);
        public static readonly TraceSource ShimController = new TraceSource(typeof(ShimController).FullName);

        /// <summary>
        /// Retrieves a list of all sources defined in this class. Uses reflection, store the result!
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<TraceSource> GetAllSources()
        {
            return typeof(V3InteropTraceSources).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => typeof(TraceSource).IsAssignableFrom(f.FieldType))
                .Select(f => (TraceSource)f.GetValue(null));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NuGet.VisualStudio.Cmdlets;

namespace NuGet.VisualStudio.Test {
    internal static class CmdletExtensions {
        public static IEnumerable<T> GetResults<T>(this NuGetBaseCmdlet cmdlet) {
            return GetResults(cmdlet).Cast<T>();
        }

        public static IEnumerable<object> GetResults(this NuGetBaseCmdlet cmdlet) {
            var result = new List<object>();
            cmdlet.CommandRuntime = new MockCommandRuntime(result);
            cmdlet.Execute();
            return result;
        }
    }
}

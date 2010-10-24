using System.Collections.Generic;
using System.Linq;
using NuPack.VisualStudio.Cmdlets;

namespace NuPack.VisualStudio.Test {
    internal static class CmdletExtensions {
        public static IEnumerable<T> GetResults<T>(this NuPackBaseCmdlet cmdlet) {
            return GetResults(cmdlet).Cast<T>();
        }

        public static IEnumerable<object> GetResults(this NuPackBaseCmdlet cmdlet) {
            var result = new List<object>();
            cmdlet.CommandRuntime = new MockCommandRuntime(result);
            cmdlet.Execute();
            return result;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace NuGet.Cmdlets.Test {
    internal static class CmdletExtensions {
        public static IEnumerable<T> GetResults<T>(this NuGetBaseCmdlet cmdlet) {
            return GetResults(cmdlet).Cast<T>();
        }

        public static IEnumerable<T> GetResults<T>(this NuGetBaseCmdlet cmdlet, List<ErrorRecord> errors, List<string> warnings) {
            return GetResults(cmdlet, errors, warnings).Cast<T>();
        }

        public static IEnumerable<object> GetResults(this NuGetBaseCmdlet cmdlet) {
            return GetResults(cmdlet, new List<ErrorRecord>(), null);
        }

        public static IEnumerable<object> GetResults(this NuGetBaseCmdlet cmdlet, List<ErrorRecord> errors, List<string> warnings) {
            var output = new List<object>();
            cmdlet.CommandRuntime = new MockCommandRuntime(output, errors, warnings);
            cmdlet.Execute();
            return output;
        }
    }
}

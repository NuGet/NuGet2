#if DEBUG
using System.IO;

namespace NuGetConsole.Host.PowerShell {
    public class DebugConstants {
        internal static string TestModulePath = Path.Combine(@"ENLISTMENT_ROOT", @"test\EndToEnd\NuGet.Tests.psd1");
    }
}
#endif
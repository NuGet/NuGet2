using System;
using System.Linq;

namespace NuGet.VisualStudio {
    public static class PackageExtensions {
        public static bool HasPowerShellScript(this IPackage package) {
            return package.HasPowerShellScript(new string[] { PowerShellScripts.Init, PowerShellScripts.Install, PowerShellScripts.Uninstall });
        }

        public static bool HasPowerShellScript(this IPackage package, string[] names) {
            return package.GetFiles().Any(file => names.Any(name => file.Path.EndsWith(name, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
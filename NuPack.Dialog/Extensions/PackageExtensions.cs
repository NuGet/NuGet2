using System;
using System.Linq;

namespace NuGet.Dialog.Providers {
    internal static class PackageExtensions {

        public static bool HasPowerShellScript(this IPackage package) {
            return package.GetFiles().Any(file => file.Path.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase));
        }
    }
}

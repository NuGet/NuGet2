using System;
using System.IO;

namespace NuGet {
    internal static class PackageUtility {
        public static bool IsManifest(string path) {
            return Path.GetExtension(path).Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAssembly(string path) {
            return path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }
    }
}

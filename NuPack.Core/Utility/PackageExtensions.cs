namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class PackageExtensions {
        public static IPackage FindByVersion(this IEnumerable<IPackage> source, Version minVersion, Version maxVersion, Version exactVersion) {
            IEnumerable<IPackage> packages = from p in source
                                             orderby p.Version descending
                                             select p;

            if (exactVersion != null) {
                // Try to match the exact version
                packages = packages.Where(p => p.Version == exactVersion);
            }
            else {
                if (minVersion != null) {
                    // Try to match the latest that satisfies the min version if any
                    packages = packages.Where(p => p.Version >= minVersion);
                }

                if (maxVersion != null) {
                    // Try to match the latest that satisfies the max version if any
                    packages = packages.Where(p => p.Version <= maxVersion);
                }
            }

            return packages.FirstOrDefault();
        }
        
        public static IEnumerable<IPackageFile> GetContentFiles(this IPackage package) {
            return package.GetFiles().Where(file => file.Path.StartsWith(Constants.ContentDirectory, StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasProjectContent(this IPackage package) {
            return package.AssemblyReferences.Any() || package.GetContentFiles().Any();
        }

        public static string GetFullName(this IPackage package) {
            return package.Id + " " + package.Version;
        }
    }
}

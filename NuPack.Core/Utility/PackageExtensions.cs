namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class PackageExtensions {
        private static readonly string ContentDir = "content";

        public static Package FindByVersion(this IQueryable<Package> source, Version minVersion, Version maxVersion, Version exactVersion) {
            IEnumerable<Package> packages = from p in source
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

        internal static bool IsDependencySatisfied(this Package package, Package targetPackage) {
            PackageDependency dependency = (from d in package.Dependencies
                                            where d.Id.Equals(targetPackage.Id, StringComparison.OrdinalIgnoreCase)
                                            select d).FirstOrDefault();

            Debug.Assert(dependency != null, "Package doesn't have this dependency");

            // Given a package's dependencies and a target package we want to see if the target package
            // satisfies the package's dependencies i.e:
            // A 1.0 -> B 1.0
            // A 2.0 -> B 2.0
            // C 1.0 -> B (>= 1.0) (min version 1.0)
            // Updating to A 2.0 from A 1.0 needs to know if there is a conflict with C
            // Since C works with B (>= 1.0) it it should be ok to update A

            // If there is an exact version specified then we check if the package is that exact version
            if (dependency.Version != null) {
                return dependency.Version.Equals(targetPackage.Version);
            }

            bool isSatisfied = true;

            // See if it meets the minimum version requirement if any
            if (dependency.MinVersion != null) {
                isSatisfied = targetPackage.Version >= dependency.MinVersion;
            }

            // See if it meets the maximum version requirement if any
            if (dependency.MaxVersion != null) {
                isSatisfied = isSatisfied && targetPackage.Version <= dependency.MaxVersion;
            }

            return isSatisfied;
        }

        internal static IEnumerable<IPackageFile> GetContentFiles(this Package package) {
            return package.GetFiles().Where(file => file.Path.StartsWith(ContentDir, StringComparison.OrdinalIgnoreCase));
        }
    }
}

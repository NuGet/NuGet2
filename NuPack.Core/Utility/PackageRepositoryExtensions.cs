namespace NuPack {
    using System;
    using System.Linq;

    public static class PackageRepositoryExtensions {
        internal static bool IsPackageInstalled(this IPackageRepository repository, IPackage package) {
            return repository.IsPackageInstalled(package.Id, package.Version);
        }

        internal static bool IsPackageInstalled(this IPackageRepository repository, string packageId, Version version = null) {
            return repository.FindPackage(packageId, null, null, version) != null;
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId) {
            return FindPackage(repository, packageId, exactVersion: null, minVersion: null, maxVersion: null);
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, Version exactVersion) {
            return FindPackage(repository, packageId, exactVersion: exactVersion, minVersion: null, maxVersion: null);
        }

        public static IPackage FindPackage(this IPackageRepository repository, string packageId, Version minVersion, Version maxVersion) {
            return FindPackage(repository, packageId, minVersion: minVersion, maxVersion: maxVersion, exactVersion: null);
        }
      
        public static IPackage FindPackage(this IPackageRepository repository, string packageId, Version minVersion, Version maxVersion, Version exactVersion) {
            if (exactVersion != null) {
                return repository.FindPackage(packageId, exactVersion);
            }
            return repository.FindPackagesById(packageId).FindByVersion(minVersion, maxVersion, exactVersion);
        }

        private static IQueryable<IPackage> FindPackagesById(this IPackageRepository repository, string packageId) {
            return from p in repository.GetPackages()
                   where p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)
                   select p;
        }        
    }
}

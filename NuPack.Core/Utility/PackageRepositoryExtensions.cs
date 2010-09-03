namespace NuPack {
    using System;
    using System.Linq;

    public static class PackageRepositoryExtensions {
        internal static bool IsPackageInstalled(this IPackageRepository repository, Package package) {
            return repository.IsPackageInstalled(package.Id, package.Version);
        }

        internal static bool IsPackageInstalled(this IPackageRepository repository, string packageId, Version version = null) {
            return repository.FindPackage(packageId, null, null, version) != null;
        }
      
        public static Package FindPackage(this IPackageRepository repository, string packageId, Version minVersion = null, Version maxVersion = null, Version exactVersion = null) {
            if (exactVersion != null) {
                return repository.FindPackage(packageId, exactVersion);
            }
            return repository.FindPackagesById(packageId).FindByVersion(minVersion, maxVersion, exactVersion);
        }

        private static IQueryable<Package> FindPackagesById(this IPackageRepository repository, string packageId) {
            return from p in repository.GetPackages()
                   where p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)
                   select p;
        }        
    }
}

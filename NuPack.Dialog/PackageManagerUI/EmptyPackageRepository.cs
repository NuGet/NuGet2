using System;
using System.Linq;

namespace NuPack.Dialog.Providers {
    // HACK: This is here since we don't have a default feed as yet so we don't have a uri
    internal class EmptyPackageRepository : IPackageRepository {
        public static readonly EmptyPackageRepository Default = new EmptyPackageRepository();
        public void AddPackage(Package package) {
            throw new NotSupportedException();
        }

        public Package FindPackage(string packageId, Version version) {
            return null;
        }

        public IQueryable<Package> GetPackages() {
            return Enumerable.Empty<Package>().AsQueryable();
        }

        public void RemovePackage(Package package) {
            throw new NotSupportedException();
        }
    }
}
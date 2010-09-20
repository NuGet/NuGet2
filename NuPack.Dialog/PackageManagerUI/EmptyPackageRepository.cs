using System;
using System.Linq;

namespace NuPack.Dialog.Providers {
    // HACK: This is here since we don't have a default feed as yet so we don't have a uri
    internal class EmptyPackageRepository : IPackageRepository {
        public static readonly EmptyPackageRepository Default = new EmptyPackageRepository();
        public void AddPackage(IPackage package) {
            throw new NotSupportedException();
        }

        public IPackage FindPackage(string packageId, Version version) {
            return null;
        }

        public IQueryable<IPackage> GetPackages() {
            return Enumerable.Empty<IPackage>().AsQueryable();
        }

        public void RemovePackage(IPackage package) {
            throw new NotSupportedException();
        }
    }
}
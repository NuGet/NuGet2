namespace NuPack {
    using System;
    using System.Linq;

    public abstract class PackageRepositoryBase : IPackageRepository {
        public abstract IQueryable<IPackage> GetPackages();

        public virtual IPackage FindPackage(string packageId, Version version) {
            return GetPackages().FirstOrDefault(p => p.Id == packageId && p.Version == version);
        }

        public virtual void AddPackage(IPackage package) {
            throw new NotSupportedException();
        }

        public virtual void RemovePackage(IPackage package) {
            throw new NotSupportedException();
        }
    }
}

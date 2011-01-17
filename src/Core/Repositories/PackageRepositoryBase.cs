namespace NuGet {
    using System;
    using System.Linq;

    public abstract class PackageRepositoryBase : IPackageRepository {

        public abstract string Source { get; }

        public abstract IQueryable<IPackage> GetPackages();

        public virtual void AddPackage(IPackage package) {
            throw new NotSupportedException();
        }

        public virtual void RemovePackage(IPackage package) {
            throw new NotSupportedException();
        }
    }
}

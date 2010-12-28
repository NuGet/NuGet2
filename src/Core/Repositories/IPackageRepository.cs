namespace NuGet {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    public interface IPackageRepository {
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call might be expensive")]
        IQueryable<IPackage> GetPackages();
        void AddPackage(IPackage package);
        void RemovePackage(IPackage package);
    }
}

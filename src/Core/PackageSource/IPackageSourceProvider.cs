using System.Collections.Generic;

namespace NuGet
{
    public interface IPackageSourceProvider
    {
        IEnumerable<PackageSource> LoadPackageSources();
        void SavePackageSources(IEnumerable<PackageSource> sources);
    }
}
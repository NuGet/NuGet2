using System.Collections.Generic;

namespace NuPack.VisualStudio {
    public interface IPackageSourceProvider {
        PackageSource ActivePackageSource { get; set; }
        IEnumerable<PackageSource> GetPackageSources();
        void AddPackageSource(PackageSource source);
        bool RemovePackageSource(PackageSource source);
        void SetPackageSources(IEnumerable<PackageSource> sources);
    }
}
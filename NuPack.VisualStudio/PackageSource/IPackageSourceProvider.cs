using System.Collections.Generic;

namespace NuGet.VisualStudio {
    public interface IPackageSourceProvider {
        PackageSource ActivePackageSource { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design", 
            "CA1024:UsePropertiesWhereAppropriate",
            Justification="This method is potentially expensive.")]
        IEnumerable<PackageSource> GetPackageSources();
        void AddPackageSource(PackageSource source);
        bool RemovePackageSource(PackageSource source);
        void SetPackageSources(IEnumerable<PackageSource> sources);
    }
}

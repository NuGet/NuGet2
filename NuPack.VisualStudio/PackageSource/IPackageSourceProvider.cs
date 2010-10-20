using System.Collections.Generic;

namespace NuPack.VisualStudio
{
    public interface IPackageSourceProvider
    {
        PackageSource ActivePackageSource { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design", 
            "CA1024:UsePropertiesWhereAppropriate",
            Justification="This method is potentially expensive because we are retrieving data from VS settings store.")]
        IEnumerable<PackageSource> GetPackageSources();

        void AddPackageSource(PackageSource source);
        bool RemovePackageSource(PackageSource source);
        void SetPackageSources(IEnumerable<PackageSource> sources);
    }
}
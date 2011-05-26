using System.Collections.Generic;

namespace NuGet.MSBuild {
    public interface IPackageReferenceProvider {
        IEnumerable<PackageReference> getPackageReferences(string packageConfigFilePath);
    }
}
using System.Collections.Generic;

namespace NuGet.MSBuild {
    class PackageReferenceProvider : IPackageReferenceProvider {
        public IEnumerable<PackageReference> getPackageReferences(string packageConfigFilePath) {
            return new PackageReferenceFile(packageConfigFilePath).GetPackageReferences(true);
        }
    }
}
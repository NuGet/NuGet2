using System.IO;

namespace NuPack.Authoring {
    public class PackageBuilderFactory : IPackageBuilderFactory {
        public IPackageBuilder CreateFrom(string path) {
            return new PackageBuilderWrapper(PackageBuilder.ReadFrom(path, Path.GetDirectoryName(path)));
        }
    }
}
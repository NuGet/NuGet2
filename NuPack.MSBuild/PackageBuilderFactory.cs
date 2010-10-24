using System;
using System.IO;

namespace NuPack.Authoring {
    public class PackageBuilderFactory : IPackageBuilderFactory {
        public IPackageBuilder CreateFrom(string path) {
            using (Stream stream = File.OpenRead(path)) {
                return new PackageBuilderWrapper(new PackageBuilder(stream));
            }
        }
    }
}
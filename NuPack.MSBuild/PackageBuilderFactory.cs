using System;
using System.IO;

namespace NuPack.Authoring {
    public class PackageBuilderFactory : IPackageBuilderFactory {
        public IPackageBuilder CreateFrom(string path) {
            return new PackageBuilder(path);
        }
    }
}
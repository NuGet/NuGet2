using System;
using System.IO;

namespace NuGet {
    public class ZipPackageFactory : IPackageFactory {
        public IPackage CreatePackage(Func<Stream> streamFactory) {
            return new ZipPackage(streamFactory, enableCaching: true);
        }

        public IPackage CreatePackage(string path) {
            return new ZipPackage(path);
        }
    }
}

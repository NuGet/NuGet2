using System;
using System.IO;
using NuGet;

namespace PackageExplorer {
    public class PackageFile : PackagePart {

        private IPackageFile _file;

        public PackageFile(IPackageFile file, string name) : base(name) {
            if (file == null) {
                throw new ArgumentNullException("file");
            }

            _file = file;
        }

        public string FullPath {
            get {
                return _file.Path;
            }
        }

        public Stream GetStream() {
            return _file.GetStream();
        }
    }
}
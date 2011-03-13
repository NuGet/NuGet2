using System;
using System.IO;
using NuGet;

namespace PackageExplorerViewModel {
    public class PhysicalFile : IPackageFile {

        private readonly string _physicalPath;

        public PhysicalFile(string physicalPath) {
            if (physicalPath == null) {
                throw new ArgumentNullException("physicalPath");
            }

            if (!File.Exists(physicalPath)) {
                throw new ArgumentException("File does not exist.", "physicalPath");
            }

            _physicalPath = physicalPath;
        }

        public string Path {
            get { return null; }
        }

        public Stream GetStream() {
            return File.OpenRead(_physicalPath);
        }
    }
}

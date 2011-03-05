using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;

namespace NuGet {
    internal class ZipPackageFile : IPackageFile {
        private Func<Stream> _streamFactory;

        public ZipPackageFile(PackagePart part) {
            Path = UriUtility.GetPath(part.Uri);
            _streamFactory = part.GetStream().ToStreamFactory();
        }

        public ZipPackageFile(IPackageFile file) {
            Path = file.Path;
            _streamFactory = file.GetStream().ToStreamFactory();
        }

        public string Path {
            get;
            private set;
        }

        public Stream GetStream() {
            return _streamFactory();
        }

        public override string ToString() {
            return Path;
        }
    }
}

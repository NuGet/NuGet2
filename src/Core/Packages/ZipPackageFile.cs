using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;

namespace NuGet {
    internal class ZipPackageFile : IPackageFile {
        private Func<MemoryStream> _streamFactory;

        public ZipPackageFile(PackagePart part) {
            Path = UriUtility.GetPath(part.Uri);

            using (Stream stream = part.GetStream()) {
                InitializeStream(stream);
            }
        }

        public ZipPackageFile(IPackageFile file) {
            Path = file.Path;

            using (Stream stream = file.GetStream()) {
                InitializeStream(stream);
            }
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

        private void InitializeStream(Stream fileStream) {
            byte[] buffer;

            using (var stream = new MemoryStream()) {
                fileStream.CopyTo(stream);
                buffer = stream.ToArray();
            }

            _streamFactory = () => new MemoryStream(buffer);
        }
    }
}

using System.IO;

namespace NuPack {
    public class PhysicalPackageFile : IPackageFile {

        public string Name {
            get;
            set;
        }

        /// <summary>
        /// Path on disk
        /// </summary>
        public string SourcePath {
            get;
            set;
        }

        /// <summary>
        /// Path in package
        /// </summary>
        public string Path {
            get;
            set;
        }

        public Stream Open() {
            return File.OpenRead(SourcePath);
        }

        public override string ToString() {
            return SourcePath;
        }
    }
}

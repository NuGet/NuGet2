using System.IO;

namespace NuPack {
    public class PhysicalPackageFile : IPackageFile {        
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
        public string TargetPath {
            get;
            set;
        }

        string IPackageFile.Path {
            get {
                return TargetPath;
            }
        }

        public Stream Open() {
            return File.OpenRead(SourcePath);
        }

        public override string ToString() {
            return TargetPath;
        }
    }
}

using System;
using System.IO;

namespace NuGet {
    public sealed class PhysicalPackageFile : IPackageFile {
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

        public Stream GetStream() {
            return File.OpenRead(SourcePath);
        }

        public override string ToString() {
            return TargetPath;
        }

        public override bool Equals(object obj) {
            var file = obj as PhysicalPackageFile;

            return file != null && String.Equals(SourcePath, file.SourcePath, StringComparison.OrdinalIgnoreCase) &&
                                   String.Equals(TargetPath, file.TargetPath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode() {
            HashCodeCombiner combiner = new HashCodeCombiner();
            combiner.AddObject(SourcePath);
            combiner.AddObject(TargetPath);
            return combiner.CombinedHash;
        }
    }
}

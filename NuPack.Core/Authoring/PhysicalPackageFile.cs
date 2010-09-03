using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace NuPack {
    public class AuthoringPackageFile : IPackageFile {

        public string Name {
            get;
            set;
        }

        public string Path {
            get;
            set;
        }

        public Func<Stream> SourceStream {
            set;
            get;
        }

        public Stream Open() {
            if (SourceStream == null) {
                throw new InvalidOperationException("Property \"SourceStream\" must be specified.");
            }
            return SourceStream();
        }
    }
}

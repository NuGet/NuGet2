using System;
using System.Collections.Generic;

namespace NuGet.VisualStudio {
    internal class VsPackageMetadata : IVsPackageMetadata {
        private readonly IPackage _package;
        private readonly string _installPath;

        public VsPackageMetadata(IPackage package, string installPath) {
            _package = package;
            _installPath = installPath;
        }

        public string Id {
            get { return _package.Id; }
        }

        public SemVer Version {
            get { return _package.Version; }
        }

        public string Title {
            get { return _package.Title; }
        }

        public IEnumerable<string> Authors {
            get { return _package.Authors; }
        }

        public string Description {
            get { return _package.Description; }
        }

        public string InstallPath {
            get { return _installPath; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet;

namespace PackageExplorerViewModel {

    public class PackageMetadata : IPackageMetadata {
        public string Id {
            get { throw new NotImplementedException(); }
        }

        public Version Version {
            get { throw new NotImplementedException(); }
        }

        public string Title {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<string> Authors {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<string> Owners {
            get { throw new NotImplementedException(); }
        }

        public Uri IconUrl {
            get { throw new NotImplementedException(); }
        }

        public Uri LicenseUrl {
            get { throw new NotImplementedException(); }
        }

        public Uri ProjectUrl {
            get { throw new NotImplementedException(); }
        }

        public bool RequireLicenseAcceptance {
            get { throw new NotImplementedException(); }
        }

        public string Description {
            get { throw new NotImplementedException(); }
        }

        public string Summary {
            get { throw new NotImplementedException(); }
        }

        public string Language {
            get { throw new NotImplementedException(); }
        }

        public string Tags {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get { throw new NotImplementedException(); }
        }
    }
}

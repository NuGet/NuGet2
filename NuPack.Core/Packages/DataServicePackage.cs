using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.IO;
using System.Linq;

namespace NuGet {
    [DataServiceKey("Id", "Version")]
    [EntityPropertyMapping("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Authors", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Summary", SyndicationItemProperty.Summary, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [CLSCompliant(false)]
    public class DataServicePackage : IPackage {
        private Lazy<IPackage> _package;
        public string Id {
            get;
            set;
        }

        public string Version {
            get;
            set;
        }

        public string Title {
            get;
            set;
        }

        public string Authors {
            get;
            set;
        }

        public Uri IconUrl {
            get;
            set;
        }

        public Uri LicenseUrl {
            get;
            set;
        }

        public Uri ProjectUrl {
            get;
            set;
        }

        public bool RequireLicenseAcceptance {
            get;
            set;
        }

        public string Description {
            get;
            set;
        }

        public string Summary {
            get;
            set;
        }

        public string Language {
            get;
            set;
        }

        public string Dependencies {
            get;
            set;
        }

        IEnumerable<string> IPackageMetadata.Authors {
            get {
                if(String.IsNullOrEmpty(Authors)) {
                    return Enumerable.Empty<string>();
                }
                return Authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies {
            get {
                return from d in Dependencies.Split(',')
                       let parts = d.Split(':')
                       where parts.Length == 4
                       select PackageDependency.CreateDependency(parts[0],
                                                                 Utility.ParseOptionalVersion(parts[1]),
                                                                 Utility.ParseOptionalVersion(parts[2]),
                                                                 Utility.ParseOptionalVersion(parts[3]));
            }
        }

        Version IPackageMetadata.Version {
            get {
                if (Version != null) {
                    return new Version(Version);
                }
                return null;
            }
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                return _package.Value.AssemblyReferences;
            }
        }
       
        public IEnumerable<IPackageFile> GetFiles() {
            return _package.Value.GetFiles();
        }

        public Stream GetStream() {
            return _package.Value.GetStream();
        }

        internal void InitializeDownloader(Func<IPackage> downloader) {
            _package = new Lazy<IPackage>(downloader, isThreadSafe: false);
        }

        public override string ToString() {
            return this.GetFullName();
        }
    }
}

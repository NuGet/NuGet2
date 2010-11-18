using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.IO;
using System.Linq;

namespace NuGet {
    [DataServiceKey("Id", "Version")]
    [EntityPropertyMapping("LastUpdated", SyndicationItemProperty.Updated, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Authors", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Summary", SyndicationItemProperty.Summary, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [CLSCompliant(false)]
    public class DataServicePackage : IPackage {
        private readonly PackageDownloader _packageDownloader = new PackageDownloader();
        private Lazy<IPackage> _package;

        public DataServicePackage() {
            _package = new Lazy<IPackage>(DownloadAndVerifyPackage, isThreadSafe: false);
        }

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

        public string Owners {
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

        public Uri ReportAbuseUrl {
            get;
            set;
        }

        public Uri DownloadUrl {
            get {
                return Context.GetReadStreamUri(this);
            }
        }

        public DateTimeOffset Published {
            get;
            set;
        }

        public DateTimeOffset LastUpdated {
            get;
            set;
        }

        public int DownloadCount {
            get;
            set;
        }

        public int Score {
            get;
            set;
        }

        public double Rating {
            get;
            set;
        }

        public int RatingsCount {
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

        public string Tags {
            get;
            set;
        }

        public string Dependencies {
            get;
            set;
        }

        public string PackageHash {
            get;
            set;
        }

        internal IDataServiceContext Context {
            get;
            set;
        }

        IEnumerable<string> IPackageMetadata.Authors {
            get {
                if (String.IsNullOrEmpty(Authors)) {
                    return Enumerable.Empty<string>();
                }
                return Authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        IEnumerable<string> IPackageMetadata.Owners {
            get {
                if (String.IsNullOrEmpty(Owners)) {
                    return Enumerable.Empty<string>();
                }
                return Owners.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies {
            get {
                if (String.IsNullOrEmpty(Dependencies)) {
                    return Enumerable.Empty<PackageDependency>();
                }
                return from d in Dependencies.Split('|')
                       let dependency = ParseDependency(d)
                       where dependency != null
                       select dependency;
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

        public override string ToString() {
            return this.GetFullName();
        }

        private IPackage DownloadAndVerifyPackage() {
            if (!String.IsNullOrEmpty(PackageHash)) {
                byte[] hashBytes = Convert.FromBase64String(PackageHash);
                return _packageDownloader.DownloadPackage(DownloadUrl, hashBytes, useCache: true);
            }
            else {
                return _packageDownloader.DownloadPackage(DownloadUrl);
            }
        }

        /// <summary>
        /// Parses a dependency from the feed in the format:
        /// id:versionSpec or id
        /// </summary>
        private static PackageDependency ParseDependency(string value) {
            if (String.IsNullOrWhiteSpace(value)) {
                return null;
            }

            string[] tokens = value.Trim().Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0) {
                return null;
            }

            // Trim the id
            string id = tokens[0].Trim();
            IVersionSpec versionSpec = null;

            if (tokens.Length > 1) {
                // Attempt to parse the version
                VersionUtility.TryParseVersionSpec(tokens[1], out versionSpec);
            }

            return new PackageDependency(id, versionSpec);
        }
    }
}
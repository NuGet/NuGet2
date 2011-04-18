using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.IO;
using System.Linq;
using NuGet.Resources;

namespace NuGet {
    [DataServiceKey("Id", "Version")]
    [EntityPropertyMapping("LastUpdated", SyndicationItemProperty.Updated, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Authors", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Summary", SyndicationItemProperty.Summary, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [CLSCompliant(false)]
    public class DataServicePackage : IPackage {
        private readonly LazyWithRecreate<IPackage> _package;

        public DataServicePackage() {
            _package = new LazyWithRecreate<IPackage>(DownloadAndVerifyPackage, ShouldUpdatePackage);
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

        public Uri GalleryDetailsUrl {
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

        internal string OldHash {
            get;
            set;
        }

        internal IDataServiceContext Context {
            get;
            set;
        }

        internal PackageDownloader Downloader { get; set; }

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

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies {
            get {
                return _package.Value.FrameworkAssemblies;
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

        private bool ShouldUpdatePackage() {
            return ShouldUpdatePackage(MachineCache.Default);
        }

        internal bool ShouldUpdatePackage(IPackageRepository repository) {
            // If the hash changed re-download the package.
            if (OldHash != PackageHash) {
                return true;
            }

            // If the package hasn't been cached, then re-download the package.
            IPackage package = GetPackage(repository);

            if (package == null) {
                return true;
            }

            // If the cached package hash isn't the same as incoming package hash
            // then re-download the package.
            string cachedHash = package.GetHash();
            if (cachedHash != PackageHash) {
                return true;
            }

            return false;
        }

        private IPackage DownloadAndVerifyPackage() {
            return DownloadAndVerifyPackage(MachineCache.Default);
        }

        internal IPackage DownloadAndVerifyPackage(IPackageRepository repository) {
            if (String.IsNullOrEmpty(PackageHash)) {
                throw new InvalidOperationException(NuGetResources.PackageContentsVerifyError);
            }

            IPackage package = null;

            // If OldHash is null, we're looking at a new instance of the data service package.
            // The package might be stored in the cache so we're going to try the looking there before attempting a download.
            if (OldHash == null) {
                package = GetPackage(repository);
            }

            if (package == null) {
                byte[] hashBytes = Convert.FromBase64String(PackageHash);

                package = Downloader.DownloadPackage(DownloadUrl, hashBytes, this);

                // Add the package to the cache
                repository.AddPackage(package);

                // Clear the cache for this package
                ZipPackage.ClearCache(package);
            }

            // Update the hash
            OldHash = PackageHash;

            return package;
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

        private IPackage GetPackage(IPackageRepository repository) {
            return repository.FindPackage(Id, ((IPackageMetadata)this).Version);
        }

        /// <summary>
        /// We can't use the built in Lazy for 2 reasons:
        /// 1. It caches the exception if any is thrown from the creator func (this means it won't retry calling the function).
        /// 2. There's no way to force a retry or expiration of the cache.
        /// </summary>
        private class LazyWithRecreate<T> {
            private readonly Func<T> _creator;
            private readonly Func<bool> _shouldRecreate;
            private T _value;
            private bool _isValueCreated;

            public LazyWithRecreate(Func<T> creator, Func<bool> shouldRecreate) {
                _creator = creator;
                _shouldRecreate = shouldRecreate;
            }

            public T Value {
                get {
                    if (_shouldRecreate() || !_isValueCreated) {
                        _value = _creator();
                        _isValueCreated = true;
                    }

                    return _value;
                }
            }
        }
    }
}
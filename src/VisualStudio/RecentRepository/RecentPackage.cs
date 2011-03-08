using System;
using System.Collections.Generic;
using System.IO;

namespace NuGet.VisualStudio {

    /// <summary>
    /// Represents a Recent package.
    /// </summary>
    public class RecentPackage : IPackage, IPersistencePackageMetadata, IEquatable<RecentPackage> {

        private readonly IPackage _basePackage;

        /// <summary>
        /// This constructor is used during serialization.
        /// </summary>
        public RecentPackage(IPackage basePackage, DateTime lastUsedDate) {
            if (basePackage == null) {
                throw new ArgumentNullException("basePackage");
            }

            _basePackage = basePackage;
            LastUsedDate = lastUsedDate;
        }

        public DateTime LastUsedDate { get; set; }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                return _basePackage.AssemblyReferences;
            }
        }

        public IEnumerable<IPackageFile> GetFiles() {
            return _basePackage.GetFiles();
        }

        public Stream GetStream() {
            return _basePackage.GetStream();
        }

        public string Id {
            get {
                return _basePackage.Id;
            }
        }

        public Version Version {
            get {
                return _basePackage.Version;
            }
        }

        public string Title {
            get {
                return _basePackage.Title;
            }
        }

        public IEnumerable<string> Authors {
            get {
                return _basePackage.Authors;
            }
        }

        public IEnumerable<string> Owners {
            get {
                return _basePackage.Owners;
            }
        }

        public Uri IconUrl {
            get {
                return _basePackage.IconUrl;
            }
        }

        public Uri LicenseUrl {
            get {
                return _basePackage.LicenseUrl;
            }
        }

        public Uri ProjectUrl {
            get {
                return _basePackage.ProjectUrl;
            }
        }

        public bool RequireLicenseAcceptance {
            get {
                return _basePackage.RequireLicenseAcceptance;
            }
        }

        public string Description {
            get {
                return _basePackage.Description;
            }
        }

        public string Summary {
            get {
                return _basePackage.Summary;
            }
        }

        public string Language {
            get {
                return _basePackage.Language;
            }
        }

        public string Tags {
            get {
                return _basePackage.Tags;
            }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get {
                return _basePackage.Dependencies;
            }
        }

        public Uri ReportAbuseUrl {
            get {
                return _basePackage.ReportAbuseUrl;
            }
        }

        public int DownloadCount {
            get {
                return _basePackage.DownloadCount;
            }
        }

        public int RatingsCount {
            get {
                return _basePackage.RatingsCount;
            }
        }

        public double Rating {
            get {
                return _basePackage.Rating;
            }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies {
            get {
                return _basePackage.FrameworkAssemblies;
            }
        }

        public bool Equals(RecentPackage other) {
            if (other == null) {
                return false;
            }

            return Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase) && Version == other.Version;
        }

        public override bool Equals(object obj) {
            return Equals(obj as RecentPackage);
        }

        public override int GetHashCode() {
            return Id.GetHashCode() * 3137 + Version.GetHashCode();
        }

        public override string ToString() {
            return Id + " " + Version + " " + LastUsedDate;
        }
    }
}
using System;
using System.Data.Services.Common;
using System.Linq;
using NuGet.Server.Infrastructure;

namespace NuGet.Server.DataServices {
    [DataServiceKey("Id", "Version")]
    [EntityPropertyMapping("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Authors", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("LastUpdated", SyndicationItemProperty.Updated, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Summary", SyndicationItemProperty.Summary, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [HasStream]
    public class Package {
        public Package(IPackage package, DerivedPackageData derivedData) {
            Id = package.Id;
            Version = package.Version.ToString();
            Title = package.Title;
            Authors = String.Join(",", package.Authors);
            if (package.IconUrl != null) {
                IconUrl = package.IconUrl.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            if (package.LicenseUrl != null) {
                LicenseUrl = package.LicenseUrl.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            if (package.ProjectUrl != null) {
                ProjectUrl = package.ProjectUrl.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            RequireLicenseAcceptance = package.RequireLicenseAcceptance;
            Description = package.Description;
            Summary = package.Summary;
            ReleaseNotes = package.ReleaseNotes;
            Tags = package.Tags;
            Dependencies = String.Join("|", from d in package.Dependencies
                                            select ConvertDependency(d));
            PackageHash = derivedData.PackageHash;
            PackageSize = derivedData.PackageSize;
            LastUpdated = derivedData.LastUpdated.UtcDateTime;
            Published = derivedData.Created.UtcDateTime;
            IsAbsoluteLatestVersion = package.IsAbsoluteLatestVersion;
            IsLatestVersion = package.IsLatestVersion;
            Path = derivedData.Path;
            FullPath = derivedData.FullPath;
        }

        internal string FullPath {
            get;
            set;
        }

        internal string Path {
            get;
            set;
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

        public string IconUrl {
            get;
            set;
        }

        public string LicenseUrl {
            get;
            set;
        }

        public string ProjectUrl {
            get;
            set;
        }

        public int DownloadCount {
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

        public string ReleaseNotes {
            get;
            set;
        }

        public DateTime Published {
            get;
            set;
        }

        public DateTime LastUpdated {
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

        public long PackageSize {
            get;
            set;
        }

        public string Copyright {
            get;
            set;
        }

        public string Tags {
            get;
            set;
        }

        public bool IsAbsoluteLatestVersion {
            get;
            set;
        }

        public bool IsLatestVersion {
            get;
            set;
        }

        public bool Listed {
            get;
            set;
        }

        private string ConvertDependency(PackageDependency dependency) {
            return String.Format("{0}:{1}", dependency.Id, dependency.VersionSpec);
        }
    }
}

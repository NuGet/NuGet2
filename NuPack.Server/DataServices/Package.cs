using System;
using System.Data.Services.Common;
using System.Linq;

namespace NuPack.Server.DataServices {
    [DataServiceKey("Id", "Version")]
    [EntityPropertyMappingAttribute("Modified", SyndicationItemProperty.Updated, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMappingAttribute("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMappingAttribute("Description", SyndicationItemProperty.Summary, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMappingAttribute("Authors", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [HasStream]
    public class Package {
        private IPackage _package;

        public Package(IPackage package) {
            _package = package;
            Id = package.Id;
            Version = package.Version.ToString();
            Category = package.Category;
            Description = package.Description;
            Language = package.Language;
            LastModifiedBy = package.LastModifiedBy;
            RequireLicenseAcceptance = package.RequireLicenseAcceptance;
            if (package.LicenseUrl != null) {
                LicenseUrl = package.LicenseUrl.GetComponents(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped);
            }
            Keywords = String.Join(", ", package.Keywords);
            Authors = String.Join(", ", package.Authors);
            Created = package.Created;
            Modified = package.Modified;
            Dependencies = String.Join(",", from d in package.Dependencies
                                            select ConvertDependency(d));
        }

        public string Id {
            get;
            set;
        }

        public string Version {
            get;
            set;
        }

        public string Category {
            get;
            set;
        }

        public string Description {
            get;
            set;
        }

        public string Language {
            get;
            set;
        }

        public string LastModifiedBy {
            get;
            set;
        }

        public bool RequireLicenseAcceptance {
            get;
            set;
        }

        public string LicenseUrl {
            get;
            set;
        }

        public string Keywords {
            get;
            set;
        }

        public string Authors {
            get;
            set;
        }

        public DateTime Created {
            get;
            set;
        }

        public DateTime Modified {
            get;
            set;
        }

        public string Dependencies {
            get;
            set;
        }

        private string ConvertDependency(PackageDependency dependency) {
            return String.Format("{0}:{1}:{2}:{3}", dependency.Id, dependency.Version, dependency.MinVersion, dependency.MaxVersion);
        }
    }
}
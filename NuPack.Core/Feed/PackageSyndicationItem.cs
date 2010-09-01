namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceModel.Syndication;

    public class PackageSyndicationItem : SyndicationItem {
        private Version _version;
        private IEnumerable<PackageDependency> _dependencies;
        private IEnumerable<string> _keywords;

        public SyndicationLink DownloadLink {
            get {
                var link = Links.Single(l => l.RelationshipType == "enclosure");
                Debug.Assert(link != null);
                return link;
            }
        }

        public string PackageId {
            get {
                return ElementExtensions.ReadElementExtensions<string>("packageId", Package.SchemaNamespace).Single();
            }
        }

        public string Description {
            get {
                TextSyndicationContent textContent = Content as TextSyndicationContent;
                if (textContent != null) {
                    return textContent.Text;
                }
                return null;
            }
        }

        public Version Version {
            get {
                if (_version == null) {
                    // Get the version string then parse it
                    string versionString = ElementExtensions.ReadElementExtensions<string>("version", Package.SchemaNamespace).Single();
                    _version = new Version(versionString);
                }
                return _version;
            }
        }

        public IEnumerable<string> Keywords {
            get {
                if (_keywords == null) {
                    _keywords = ElementExtensions.ReadElementExtensions<string[]>("keywords", Package.SchemaNamespace).SingleOrDefault()
                                ?? Enumerable.Empty<string>();
                }
                return _keywords;
            }
        }

        public string Language {
            get {
                return ElementExtensions.ReadElementExtensions<string>("language", Package.SchemaNamespace).SingleOrDefault();
            }
        }

        public string LastModifiedBy {
            get {
                return ElementExtensions.ReadElementExtensions<string>("lastModifiedBy", Package.SchemaNamespace).SingleOrDefault();
            }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get {
                if (_dependencies == null) {
                    var dependencies = ElementExtensions.ReadElementExtensions<PackageFeedDependency[]>("dependencies", Package.SchemaNamespace).SingleOrDefault();
                    if (dependencies != null) {
                        _dependencies = dependencies.Select(d => d.ToPackageDependency());
                    }
                    else {
                        _dependencies = Enumerable.Empty<PackageDependency>();
                    }
                }
                return _dependencies;
            }
        }
    }
}

namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel.Syndication;

    public class PackageSyndicationItem : SyndicationItem {
        private Version _version;
        private IEnumerable<PackageDependency> _dependencies;
        private string _packageId;
        private IEnumerable<string> _keywords;
        
        public string PackageId {
            get {
                if (_packageId == null) {
                    _packageId = ParsePackageId(Id);
                }
                return _packageId;
            }
        }

        public Uri SourceUrl {
            get {
                return Links[0].Uri;
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
                    if(dependencies != null) {
                        _dependencies = dependencies.Select(d => d.ToPackageDependency());
                    }
                    else {
                        _dependencies = Enumerable.Empty<PackageDependency>();
                    }           
                }
                return _dependencies;
            }
        }
        
        private static string ParsePackageId(string entryId) {
            // Entry Id format
            // uuid:{guid};id={packageId}
            return entryId.Split(';')[1].Substring(3);
        }
    }
}

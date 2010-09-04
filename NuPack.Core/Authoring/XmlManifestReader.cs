using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NuPack {
    internal class XmlManifestReader {
        private XDocument _manifestFile;

        public XmlManifestReader(string manifestFile) {
            _manifestFile = XDocument.Load(manifestFile);
            BasePath = Path.GetDirectoryName(manifestFile);
        }

        public XmlManifestReader(XDocument document) {
            _manifestFile = document;
        }

        /// <summary>
        /// Used to resolve relative paths. 
        /// Assigned the manifest file's path when we receieve a file to start with. 
        /// </summary>
        public string BasePath {
            get;
            set;
        }

        public virtual void ReadContentTo(PackageBuilder builder) {
            ReadMetaData(builder);
            ReadDependencies(builder);
            ReadFiles(builder);
        }

        private void ReadMetaData(PackageBuilder builder) {
            XElement metadataElement = _manifestFile.Root.Element("Metadata");

            if (metadataElement.Element("Identifier") != null) {
                builder.Id = metadataElement.Element("Identifier").Value;
            }
            if (metadataElement.Element("Version") != null) {
                Version version = null;
                Version.TryParse(metadataElement.Element("Version").Value, out version);
                builder.Version = version;
            }

            builder.Description = metadataElement.GetOptionalElementValue("Description");
            var authorsElement = metadataElement.Element("Authors");
            if (authorsElement != null) {
                builder.Authors.AddRange(from e in authorsElement.Elements("Author") select e.Value);
            }

            builder.Language = metadataElement.GetOptionalElementValue("Language");
            DateTime created;
            if (DateTime.TryParse(metadataElement.GetOptionalElementValue("Created"), out created)) {
                builder.Created = created;
            }
            DateTime modified;
            if (DateTime.TryParse(metadataElement.GetOptionalElementValue("Modified"), out modified)) {
                builder.Modified = modified;
            }
            builder.LastModifiedBy = metadataElement.GetOptionalElementValue("LastModifiedBy");
            builder.Category = metadataElement.GetOptionalElementValue("Category");
            builder.Keywords.AddRange((metadataElement.GetOptionalElementValue("Keywords") ?? String.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private void ReadDependencies(PackageBuilder builder) {
            var dependenciesElement = _manifestFile.Root.Element("Dependencies");
            if (dependenciesElement != null) {
                var dependenices = from item in dependenciesElement.Elements()
                                   select ReadPackageDepedency(item);
                foreach (var item in dependenices) {
                    builder.Dependencies.Add(item);
                }
            }
        }

        private static PackageDependency ReadPackageDepedency(XElement item) {
            var id = item.Attribute("id").Value;
            Version version = null, minVersion = null, maxVersion = null;

            var versionString = item.GetOptionalAttributeValue("version");
            if (!String.IsNullOrEmpty(versionString)) {
                Version.TryParse(versionString, out version);
            }

            versionString = item.GetOptionalAttributeValue("minversion");
            if (!String.IsNullOrEmpty(versionString)) {
                Version.TryParse(versionString, out minVersion);
            }

            versionString = item.GetOptionalAttributeValue("maxversion");
            if (!String.IsNullOrEmpty(versionString)) {
                Version.TryParse(versionString, out maxVersion);
            }

            return PackageDependency.CreateDependency(id, minVersion, maxVersion, version);
        }

        private void ReadFiles(PackageBuilder builder) {
            // Do nothing with files if the base path is null.
            if (String.IsNullOrEmpty(BasePath)) {
                return;
            }

            var files = _manifestFile.Root.Element("Files");
            if (files != null) {
                // REVIEW: Should we look for specific elements?
                foreach (var file in files.Elements()) {
                    var source = file.GetOptionalAttributeValue("src");
                    var destination = file.GetOptionalAttributeValue("dest");
                    if (!String.IsNullOrEmpty(source)) {
                        AddFiles(builder, source, destination);
                    }
                }
            }
            else {
                // No files element so assume we want to package everything recursively from the manifest root
                AddFiles(builder, @"**\*.*", null);
            }
        }

        private void AddFiles(PackageBuilder builder, string source, string destination) {
            PathSearchFilter searchFilter = PathResolver.ResolvePath(BasePath, source);
            foreach (var file in Directory.EnumerateFiles(searchFilter.SearchDirectory, searchFilter.SearchPattern, searchFilter.SearchOption)) {
                var destinationPath = PathResolver.ResolvePackagePath(BasePath, file, destination);
                builder.Files.Add(new PhysicalPackageFile {
                    Name = Path.GetFileName(file),
                    SourcePath = file,
                    Path = destinationPath
                });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.Versioning;


namespace NuPack {
    public class XmlManifestReader {
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
            ReadReferences(builder);
            foreach (int value in Enum.GetValues(typeof(PackageFileType))) {
                var key = (PackageFileType)value;
                ReadPackageFiles(builder, key);
            }
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

            builder.Category = metadataElement.GetOptionalElementValue("Category");
            builder.Keywords.AddRange((metadataElement.GetOptionalElementValue("Keywords") ?? String.Empty).Split(','));
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

        private void ReadReferences(PackageBuilder builder) {
            var assemblies = _manifestFile.Root.Element("Assemblies");
            if (assemblies != null) {

                foreach (var item in assemblies.Elements()) {
                    foreach (var reference in ReadAssemblyReferences(item)) {
                        builder.References.Add(reference);
                    }
                }
            }
        }

        private IEnumerable<PhysicalAssemblyReference> ReadAssemblyReferences(XElement item) {
            var src = item.GetOptionalAttributeValue("src");
            if (String.IsNullOrEmpty(src)) {
                return Enumerable.Empty<PhysicalAssemblyReference>();
            }
            var frameworkVersionString = item.GetOptionalAttributeValue("TargetFramework");
            
            FrameworkName frameworkVersion = null;
            if (frameworkVersionString != null) {
                frameworkVersion = new FrameworkName(frameworkVersionString);
            }
            var name = item.GetOptionalAttributeValue("name");
            var searchFilter = PathResolver.ResolvePath(BasePath, src);

            return 
                from file in Directory.EnumerateFiles(searchFilter.SearchDirectory, searchFilter.SearchPattern, searchFilter.SearchOption)
                select new PhysicalAssemblyReference {
                    SourcePath = file,
                    Name = name ?? Path.GetFileNameWithoutExtension(file),
                    Path = Path.GetFileName(file), // The package builder would force drop this in a location determined by version
                    TargetFramework = frameworkVersion
                };

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

        private void ReadPackageFiles(PackageBuilder builder, PackageFileType fileType) {
            var packageFiles = _manifestFile.Root.Element(fileType.ToString());
            if (packageFiles != null) {
                foreach (var file in packageFiles.Elements()) {
                    var source = file.GetOptionalAttributeValue("src");
                    var destination = file.GetOptionalAttributeValue("dest");
                    if (!String.IsNullOrEmpty(source)) {
                        AddFilesFromSource(builder, fileType, source, destination);
                    }
                }
            }
        }

        private void AddFilesFromSource(PackageBuilder builder, PackageFileType fileType, string source, string destination) {
            var fileList = builder.GetFiles(fileType);

            PathSearchFilter searchFilter = PathResolver.ResolvePath(BasePath, source);
            foreach(var file in Directory.EnumerateFiles(searchFilter.SearchDirectory, searchFilter.SearchPattern, searchFilter.SearchOption)) {
                var destinationPath = PathResolver.ResolvePackagePath(BasePath, file, destination);
                fileList.Add(new PhysicalPackageFile { Name = Path.GetFileName(file), SourcePath = file, Path = destinationPath });
            }
        }
    }
}

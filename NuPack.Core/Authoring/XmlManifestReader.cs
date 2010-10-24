using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace NuPack {
    [XmlType("package", Namespace = Constants.ManifestSchemaNamespace)]
    public class Manifest {
        public Manifest() {
            Metadata = new ManifestMetadata();
        }
        [XmlElement("metadata", IsNullable = false)]
        public ManifestMetadata Metadata { get; set; }

        [XmlArray("files")]
        [XmlArrayItem("file")]
        public List<ManifestFile> Files { get; set; }

        public void Save(Stream stream) {
            var serializer = new XmlSerializer(typeof(Manifest));
            serializer.Serialize(stream, this);
        }
    }

    [XmlType("file", Namespace = Constants.ManifestSchemaNamespace)]
    public class ManifestFile {
        [XmlElement("src")]
        public string Source { get; set; }

        [XmlElement("target")]
        public string Target { get; set; }
    }

    [XmlType("metadata", Namespace = Constants.ManifestSchemaNamespace)]
    public class ManifestMetadata : IPackageMetadata {
        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("authors")]
        public string Authors { get; set; }

        [XmlElement("licenseUrl")]
        public string LicenseUrl { get; set; }

        [XmlElement("projectUrl")]
        public string ProjectUrl { get; set; }

        [XmlElement("iconUrl")]
        public string IconUrl { get; set; }

        [XmlElement("requireLicenseAcceptance")]
        public bool RequireLicenseAcceptance { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("summary")]
        public string Summary { get; set; }

        [XmlElement("language")]
        public string Language { get; set; }

        [XmlArray("dependencies")]
        [XmlArrayItem("dependency")]
        public List<ManifestDependency> Dependencies { get; set; }

        Version IPackageMetadata.Version {
            get {
                if (Version == null) {
                    return null;
                }
                return new Version(Version);
            }
        }

        Uri IPackageMetadata.IconUrl {
            get {
                if (IconUrl == null) {
                    return null;
                }
                return new Uri(IconUrl);
            }
        }

        Uri IPackageMetadata.LicenseUrl {
            get {
                if (LicenseUrl == null) {
                    return null;
                }
                return new Uri(LicenseUrl);
            }
        }

        Uri IPackageMetadata.ProjectUrl {
            get {
                if (ProjectUrl == null) {
                    return null;
                }
                return new Uri(ProjectUrl);
            }
        }

        IEnumerable<string> IPackageMetadata.Authors {
            get {
                if (String.IsNullOrEmpty(Authors)) {
                    return Enumerable.Empty<string>();
                }
                return Authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies {
            get {
                return from dependency in Dependencies
                       select PackageDependency.CreateDependency(dependency.Id,
                                                                 Utility.ParseOptionalVersion(dependency.MinVersion),
                                                                 Utility.ParseOptionalVersion(dependency.MaxVersion),
                                                                 Utility.ParseOptionalVersion(dependency.Version));
            }
        }
    }

    [XmlType("dependency", Namespace = Constants.ManifestSchemaNamespace)]
    public class ManifestDependency {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("minVersion")]
        public string MinVersion { get; set; }

        [XmlAttribute("maxVersion")]
        public string MaxVersion { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }
    }

    /*internal class XmlManifestReader {
        private XDocument _manifestDocument;

        public XmlManifestReader(string manifestFile) {
            _manifestDocument = XDocument.Load(manifestFile);
            ValidateSchema(_manifestDocument);
            BasePath = Path.GetDirectoryName(manifestFile);
        }

        public XmlManifestReader(XDocument document) {
            ValidateSchema(document);
            _manifestDocument = document;
        }

        /// <summary>
        /// Used to resolve relative paths. 
        /// Assigned the manifest file's path when we receieve a file to start with. 
        /// </summary>
        public string BasePath {
            get;
            private set;
        }

        public virtual void ReadContentTo(PackageBuilder builder) {
            ReadMetadata(builder);
            ReadDependencies(builder);
            ReadFiles(builder);
        }


        private static void EnsureNamespace(XElement element) {            
            // This method recursively goes through all descendants and makes sure it's in the nuspec namespace.
            // Namespaces are hard to type by hand so we don't want to require it, but we can 
            // transform the document in memory before validation if the namespace wasn't specified.
            if (String.IsNullOrEmpty(element.Name.NamespaceName)) {
                element.Name = MakeName(element.Name.LocalName);
            }

            foreach (var childElement in element.Descendants()) {
                if (String.IsNullOrEmpty(childElement.Name.NamespaceName)) {
                    childElement.Name = MakeName(childElement.Name.LocalName);
                }
            }
        }

        internal static void ValidateSchema(XDocument document) {
            if (document.Root != null) {
                EnsureNamespace(document.Root);
            }

            // Get the xsd from the assembly
            var stream = typeof(XmlManifestReader).Assembly.GetManifestResourceStream("NuPack.Authoring.nuspec.xsd");
            Debug.Assert(stream != null);

            // Validate the document against the xsd schema
            using (StreamReader reader = new StreamReader(stream)) {
                XmlSchemaSet schemaSet = new XmlSchemaSet();
                using (var stringReader = new StringReader(reader.ReadToEnd())) {
                    XmlReader schemaReader = XmlReader.Create(stringReader);
                    schemaSet.Add(Constants.ManifestSchemaNamespace, schemaReader);
                    document.Validate(schemaSet, OnValidate);
                }
            }
        }

        private static void OnValidate(object sender, ValidationEventArgs e) {
            if (e.Severity == XmlSeverityType.Error) {
                // Throw an exception if there is a validation error
                throw new InvalidOperationException(e.Message);
            }
        }

        private void ReadMetadata(PackageBuilder builder) {
            XElement metadataElement = _manifestDocument.Root.Element(MakeName("metadata"));

            builder.Id = metadataElement.Element(MakeName("id")).Value;
            string versionString = metadataElement.Element(MakeName("version")).Value;
            // This will fail if the version is invalid
            builder.Version = new Version(versionString);
            builder.Description = metadataElement.Element(MakeName("description")).Value;

            XElement authorsElement = metadataElement.Element(MakeName("authors"));
            builder.Authors.AddRange(from e in authorsElement.Elements(MakeName("author")) 
                                            select e.Value);

            DateTime created;
            if (DateTime.TryParse(metadataElement.GetOptionalElementValue("created", Constants.ManifestSchemaNamespace), out created)) {
                builder.Created = created;
            }
            DateTime modified;
            if (DateTime.TryParse(metadataElement.GetOptionalElementValue("modified", Constants.ManifestSchemaNamespace), out modified)) {
                builder.Modified = modified;
            }
            string licenseUrl = metadataElement.GetOptionalElementValue("licenseUrl", Constants.ManifestSchemaNamespace);
            if (!String.IsNullOrEmpty(licenseUrl)) {
                builder.LicenseUrl = new Uri(licenseUrl);
            }
            bool requireLicenseAcceptance;
            if (Boolean.TryParse(metadataElement.GetOptionalElementValue("requireLicenseAcceptance", Constants.ManifestSchemaNamespace), out requireLicenseAcceptance)) {
                builder.RequireLicenseAcceptance = requireLicenseAcceptance;
            }
            builder.Language = metadataElement.GetOptionalElementValue("language", Constants.ManifestSchemaNamespace);
            builder.LastModifiedBy = metadataElement.GetOptionalElementValue("lastmodifiedby", Constants.ManifestSchemaNamespace);
            builder.Category = metadataElement.GetOptionalElementValue("category", Constants.ManifestSchemaNamespace);
            string keywords = metadataElement.GetOptionalElementValue("keywords", Constants.ManifestSchemaNamespace);

            if (!String.IsNullOrWhiteSpace(keywords)) {
                builder.Keywords.AddRange(keywords.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        private static XName MakeName(string name) {
            return XName.Get(name, Constants.ManifestSchemaNamespace);
        }

        private void ReadDependencies(PackageBuilder builder) {
            XElement dependencies = _manifestDocument.Root.Element(MakeName("dependencies"));
            if (dependencies != null) {
                builder.Dependencies.AddRange(from dependency in dependencies.Elements(MakeName("dependency"))
                                              select ReadPackageDepedency(dependency));
            }
        }

        private static PackageDependency ReadPackageDepedency(XElement dependency) {
            var id = dependency.Attribute("id").Value;

            Version version = null, minVersion = null, maxVersion = null;

            var versionString = dependency.GetOptionalAttributeValue("version");
            if (!String.IsNullOrEmpty(versionString)) {
                if (!Version.TryParse(versionString, out version)) {
                    version = null;
                }
            }

            versionString = dependency.GetOptionalAttributeValue("minversion");
            if (!String.IsNullOrEmpty(versionString)) {
                if (!Version.TryParse(versionString, out minVersion)) {
                    minVersion = null;
                }
            }

            versionString = dependency.GetOptionalAttributeValue("maxversion");
            if (!String.IsNullOrEmpty(versionString)) {
                if (!Version.TryParse(versionString, out maxVersion)) {
                    maxVersion = null;
                }
            }

            return PackageDependency.CreateDependency(id, minVersion, maxVersion, version);
        }

        private void ReadFiles(PackageBuilder builder) {
            // Do nothing with files if the base path is null (empty means current directory)
            if (BasePath == null) {
                return;
            }

            var files = _manifestDocument.Root.Elements(MakeName("files"));
            if (files.Any()) {
                foreach (var file in files) {
                    var source = file.Attribute("src").Value;
                    var destination = file.GetOptionalAttributeValue("target");
                    AddFiles(builder, source, destination);
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
                var destinationPath = PathResolver.ResolvePath(source, BasePath, file, destination);
                builder.Files.Add(new PhysicalPackageFile {
                    SourcePath = file,
                    TargetPath = destinationPath
                });
            }
        }
    }*/
}

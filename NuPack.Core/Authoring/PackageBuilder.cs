using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace NuGet {
    public class PackageBuilder : IPackageBuilder {
        private const string DefaultContentType = "application/octet";
        internal const string ManifestRelationType = "manifest";

        public PackageBuilder(string path)
            : this(path, Path.GetDirectoryName(path)) {

        }

        public PackageBuilder(string path, string basePath)
            : this() {
            using (Stream stream = File.OpenRead(path)) {
                ReadManifest(stream, basePath);
            }
        }


        public PackageBuilder(Stream stream, string basePath)
            : this() {
            ReadManifest(stream, basePath);
        }

        public PackageBuilder() {
            Files = new Collection<IPackageFile>();
            Dependencies = new Collection<PackageDependency>();
            Authors = new Collection<string>();
        }

        public string Id {
            get;
            set;
        }

        public Version Version {
            get;
            set;
        }

        public string Title {
            get;
            set;
        }

        public Collection<string> Authors {
            get;
            private set;
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

        public Collection<PackageDependency> Dependencies {
            get;
            private set;
        }

        public Collection<IPackageFile> Files {
            get;
            private set;
        }

        IEnumerable<string> IPackageMetadata.Authors {
            get {
                return Authors;
            }
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies {
            get {
                return Dependencies;
            }
        }

        public void Save(Stream stream) {
            using (Package package = Package.Open(stream, FileMode.Create)) {
                WriteManifest(package);
                WriteFiles(package);

                // Copy the metadata properties back to the package
                package.PackageProperties.Creator = String.Join(",", Authors);
                package.PackageProperties.Description = Description;
                package.PackageProperties.Identifier = Id;
                package.PackageProperties.Version = Version.ToString();
                package.PackageProperties.Language = Language;
            }
        }

        private void ReadManifest(Stream stream, string basePath) {
            // Load an xml document
            XDocument document = XDocument.Load(stream);

            // Validate the manifest
            ValidateManifest(document);

            // Remove the Deserialize the document
            document.Root.Name = document.Root.Name.LocalName;

            // Deserialize the document and extract the metadata
            var serializer = new XmlSerializer(typeof(Manifest));
            Manifest manifest = (Manifest)serializer.Deserialize(document.CreateReader());
            IPackageMetadata metadata = manifest.Metadata;

            Id = metadata.Id;
            Version = metadata.Version;
            Title = metadata.Title;
            Authors.AddRange(metadata.Authors);
            IconUrl = metadata.IconUrl;
            LicenseUrl = metadata.LicenseUrl;
            ProjectUrl = metadata.ProjectUrl;
            RequireLicenseAcceptance = metadata.RequireLicenseAcceptance;
            Description = metadata.Description;
            Summary = metadata.Summary;
            Language = metadata.Language;
            Dependencies.AddRange(metadata.Dependencies);

            // If there's no base path then ignore the files node
            if (basePath != null) {
                if (manifest.Files == null || !manifest.Files.Any()) {
                    AddFiles(basePath, @"**\*.*", null);
                }
                else {
                    foreach (var file in manifest.Files) {
                        AddFiles(basePath, file.Source, file.Target);
                    }
                }
            }
        }

        private static void ValidateManifest(XDocument document) {
            // Create the schema set
            var schemaSet = new XmlSchemaSet();
            using (Stream schemaStream = GetSchemaStream()) {
                schemaSet.Add(Constants.ManifestSchemaNamespace, XmlReader.Create(schemaStream));
            }

            // Add the namespace to the document so we can validate it against the xsd
            EnsureNamespace(document.Root);

            // Validate the document
            document.Validate(schemaSet, (sender, e) => {
                if (e.Severity == XmlSeverityType.Error) {
                    // Throw an exception if there is a validation error
                    throw new InvalidOperationException(e.Message);
                }
            });
        }

        private static Stream GetSchemaStream() {
            return typeof(PackageBuilder).Assembly.GetManifestResourceStream("NuGet.Authoring.nuspec.xsd");
        }

        private static void EnsureNamespace(XElement element) {
            // This method recursively goes through all descendants and makes sure it's in the nuspec namespace.
            // Namespaces are hard to type by hand so we don't want to require it, but we can 
            // transform the document in memory before validation if the namespace wasn't specified.
            if (String.IsNullOrEmpty(element.Name.NamespaceName)) {
                element.Name = FullyQualifyName(element.Name.LocalName);
            }

            foreach (var childElement in element.Descendants()) {
                if (String.IsNullOrEmpty(childElement.Name.NamespaceName)) {
                    childElement.Name = FullyQualifyName(childElement.Name.LocalName);
                }
            }
        }

        private static XName FullyQualifyName(string name) {
            return XName.Get(name, Constants.ManifestSchemaNamespace);
        }

        private void WriteManifest(Package package) {
            Uri uri = UriHelper.CreatePartUri(Id + Constants.ManifestExtension);

            // Create the manifest relationship
            package.CreateRelationship(uri, TargetMode.Internal, Constants.SchemaNamespace + ManifestRelationType);

            // Create the part
            PackagePart packagePart = package.CreatePart(uri, DefaultContentType, CompressionOption.Maximum);

            using (Stream stream = packagePart.GetStream()) {
                var serializer = new XmlSerializer(typeof(Manifest));
                Manifest manifest = Manifest.Create(this);
                serializer.Serialize(stream, manifest);
            }

            // Validate the document
            using (Stream stream = packagePart.GetStream()) {
                ValidateManifest(XDocument.Load(stream));
            }
        }

        private void WriteFiles(Package package) {
            // Add files that might not come from expanding files on disk
            foreach (IPackageFile file in Files) {
                using (Stream stream = file.GetStream()) {
                    CreatePart(package, file.Path, stream);
                }
            }
        }

        private void AddFiles(string basePath, string source, string destination) {
            PathSearchFilter searchFilter = PathResolver.ResolvePath(basePath, source);
            foreach (var file in Directory.EnumerateFiles(searchFilter.SearchDirectory,
                                                          searchFilter.SearchPattern,
                                                          searchFilter.SearchOption)) {
                var destinationPath = PathResolver.ResolvePackagePath(source, basePath, file, destination);
                Files.Add(new PhysicalPackageFile {
                    SourcePath = file,
                    TargetPath = destinationPath
                });
            }
        }

        private static void CreatePart(Package package, string path, Stream sourceStream) {
            if (Utility.IsManifest(path)) {
                return;
            }

            Uri uri = UriHelper.CreatePartUri(path);

            // Create the part
            PackagePart packagePart = package.CreatePart(uri, DefaultContentType, CompressionOption.Maximum);
            using (Stream stream = packagePart.GetStream()) {
                sourceStream.CopyTo(stream);
            }
        }
    }
}

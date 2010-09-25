namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Linq;
    using Opc = System.IO.Packaging;

    public class PackageBuilder {
        private const string DefaultContentType = "application/octet";
        internal const string ManifestRelationType = "manifest";

        public PackageBuilder() {
            Files = new List<IPackageFile>();
            Dependencies = new List<PackageDependency>();
            Keywords = new List<string>();
            Authors = new List<string>();
        }

        public string Id {
            get;
            set;
        }

        public string Description {
            get;
            set;
        }

        public List<string> Authors {
            get;
            private set;
        }

        public List<string> Keywords {
            get;
            private set;
        }

        public string Category {
            get;
            set;
        }

        public Version Version {
            get;
            set;
        }

        public DateTime Created {
            get;
            set;
        }

        public string Language {
            get;
            set;
        }

        public Uri LicenseUrl {
            get;
            set;
        }

        public DateTime Modified {
            get;
            set;
        }

        public string LastModifiedBy {
            get;
            set;
        }

        public List<PackageDependency> Dependencies {
            get;
            private set;
        }

        public List<IPackageFile> Files {
            get;
            private set;
        }

        public static void Save(IPackage package, Stream stream) {
            ReadFrom(package).Save(stream);
        }

        public void Save(Stream stream) {
            using (Opc.Package package = Opc.Package.Open(stream, FileMode.Create)) {
                WriteManifest(package);
                WriteFiles(package);

                // Copy the metadata properties back to the package
                package.PackageProperties.Category = Category;
                package.PackageProperties.Created = Created;
                package.PackageProperties.Creator = String.Join(", ", Authors);
                package.PackageProperties.Description = Description;
                package.PackageProperties.Identifier = Id;
                package.PackageProperties.Version = Version.ToString();
                package.PackageProperties.Keywords = String.Join(", ", Keywords);
                package.PackageProperties.LastModifiedBy = LastModifiedBy;
                package.PackageProperties.Modified = Modified;
                package.PackageProperties.Language = Language;
            }
        }

        public static PackageBuilder ReadFrom(Stream stream) {
            XmlManifestReader reader = new XmlManifestReader(XDocument.Load(stream));
            PackageBuilder builder = new PackageBuilder();
            reader.ReadContentTo(builder);
            return builder;
        }

        public static PackageBuilder ReadFrom(string path) {
            XmlManifestReader reader = new XmlManifestReader(path);
            PackageBuilder builder = new PackageBuilder();
            reader.ReadContentTo(builder);
            return builder;
        }

        public static PackageBuilder ReadFrom(IPackage package) {
            PackageBuilder packageBuilder = new PackageBuilder();

            // Copy meta data
            packageBuilder.Authors.AddRange(package.Authors);
            packageBuilder.Category = package.Category;
            packageBuilder.Created = package.Created;
            packageBuilder.Description = package.Description;
            packageBuilder.Id = package.Id;
            packageBuilder.Keywords.AddRange(package.Keywords);
            packageBuilder.Version = package.Version;
            packageBuilder.LastModifiedBy = package.LastModifiedBy;
            packageBuilder.Language = package.Language;
            packageBuilder.Modified = package.Modified;
            packageBuilder.LicenseUrl = package.LicenseUrl;

            // Copy dependencies
            packageBuilder.Dependencies.AddRange(package.Dependencies);

            // Copy files
            packageBuilder.Files.AddRange(package.GetFiles());

            // Remove the manifest file
            packageBuilder.Files.RemoveAll(file => Utility.IsManifest(file.Path));

            return packageBuilder;
        }

        private void WriteManifest(Opc.Package package) {
            Uri uri = UriHelper.CreatePartUri(Id + Constants.ManifestExtension);

            // Create the manifest relationship
            package.CreateRelationship(uri, Opc.TargetMode.Internal, Constants.SchemaNamespace + ManifestRelationType);

            // Create the part
            Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);

            using (Stream stream = packagePart.GetStream()) {
                var writer = new XmlManifestWriter(this);
                writer.Save(stream);
            }

            // We need to reopen the stream after we've written the manifest so we can validate it
            using (Stream stream = packagePart.GetStream()) {
                XmlManifestReader.ValidateSchema(XDocument.Load(stream));
            }
        }

        private void WriteFiles(Opc.Package package) {
            foreach (var file in Files) {
                using (Stream stream = file.Open()) {
                    CreatePart(package, file.Path, stream);
                }
            }
        }

        private static void CreatePart(Opc.Package package, string packagePath, Stream sourceStream) {
            Uri uri = UriHelper.CreatePartUri(packagePath);

            // Create the part
            Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);

            using (Stream stream = packagePart.GetStream()) {
                sourceStream.CopyTo(stream);
            }
        }
    }
}

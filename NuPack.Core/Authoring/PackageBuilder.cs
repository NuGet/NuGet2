using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using NuPack.Resources;
using Opc = System.IO.Packaging;

namespace NuPack {
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

        public void Save(Stream stream) {
            VerifyPackage();
            WritePackageContent(stream);
        }

        private void VerifyPackage() {
            if (String.IsNullOrEmpty(Id) || Version == null) {
                throw new InvalidOperationException(NuPackResources.PackageBuilder_IdAndVersionRequired);
            }
        }

        public static void Save(Package package, Stream stream) {
            ReadFrom(package).Save(stream);
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

        public static PackageBuilder ReadFrom(Package package) {
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

            // Copy dependencies
            packageBuilder.Dependencies.AddRange(package.Dependencies);

            // Copy files
            packageBuilder.Files.AddRange(package.GetFiles());

            // Remove the manifest file
            packageBuilder.Files.RemoveAll(file => Utility.IsManifest(file.Path));

            return packageBuilder;
        }

        private void WritePackageContent(Stream stream) {
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

        private void WriteManifest(Opc.Package package) {
            Uri uri = UriHelper.CreatePartUri(Id + Utility.ManifestExtension);

            // Create the manifest relationship
            package.CreateRelationship(uri, Opc.TargetMode.Internal, Package.SchemaNamespace + ManifestRelationType);

            // Create the part
            Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);

            using (Stream outStream = packagePart.GetStream()) {
                var writer = new XmlManifestWriter(this);
                writer.Save(outStream);
            }
        }

        private void WriteFiles(Opc.Package package) {
            foreach (var file in Files) {
                using (Stream stream = file.Open()) {
                    CreatePart(package, file.Path, stream);
                }
            }
        }

        private void CreatePart(Opc.Package package, string packagePath, Stream sourceStream) {
            Uri uri = UriHelper.CreatePartUri(packagePath);

            // Create the part
            Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);

            using (Stream outStream = packagePart.GetStream()) {
                sourceStream.CopyTo(outStream);
            }
        }
    }
}

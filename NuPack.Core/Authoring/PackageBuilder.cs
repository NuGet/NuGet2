using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml.Serialization;

namespace NuPack {
    public class PackageBuilder {
        private const string DefaultContentType = "application/octet";
        internal const string ManifestRelationType = "manifest";

        public PackageBuilder(Stream stream) {
            var serializer = new XmlSerializer(typeof(Manifest));
            Manifest = (Manifest)serializer.Deserialize(stream);
            Files = new Collection<IPackageFile>();
        }

        public PackageBuilder()
            : this(new Manifest()) {
        }

        public PackageBuilder(Manifest manifest) {
            Manifest = manifest;
            Files = new Collection<IPackageFile>();
        }

        public Manifest Manifest {
            get;
            private set;
        }

        public Collection<IPackageFile> Files {
            get;
            private set;
        }

        public void Save(Stream stream) {
            Save(stream, basePath: null);
        }

        public void Save(Stream stream, string basePath) {
            using (Package package = Package.Open(stream, FileMode.Create)) {
                WriteManifest(package);
                WriteFiles(package, basePath);

                // Copy the metadata properties back to the package
                package.PackageProperties.Creator = Manifest.Metadata.Authors;
                package.PackageProperties.Description = Manifest.Metadata.Description;
                package.PackageProperties.Identifier = Manifest.Metadata.Id;
                package.PackageProperties.Version = Manifest.Metadata.Version;
                package.PackageProperties.Language = Manifest.Metadata.Language;
            }
        }

        private void WriteManifest(Package package) {
            Uri uri = UriHelper.CreatePartUri(Manifest.Metadata.Id + Constants.ManifestExtension);

            // Create the manifest relationship
            package.CreateRelationship(uri, TargetMode.Internal, Constants.SchemaNamespace + ManifestRelationType);

            // Create the part
            PackagePart packagePart = package.CreatePart(uri, DefaultContentType, CompressionOption.Maximum);

            using (Stream stream = packagePart.GetStream()) {
                // TODO: Validate xsd
                var serializer = new XmlSerializer(typeof(Manifest));
                serializer.Serialize(stream, Manifest);
            }
        }

        private void WriteFiles(Package package, string basePath) {
            // If there's no base path then ignore the files node
            if (!String.IsNullOrEmpty(basePath)) {
                if (Manifest.Files != null && !Manifest.Files.Any()) {
                    AddFiles(package, basePath, @"**\*.*", null);
                }
                else {
                    foreach (var file in Manifest.Files) {
                        AddFiles(package, basePath, file.Source, file.Target);
                    }
                }
            }

            // Add files that might not come from expanding files on disk
            foreach (IPackageFile file in Files) {
                using (Stream stream = file.GetStream()) {
                    CreatePart(package, file.Path, stream);
                }
            }
        }

        private void AddFiles(Package package, string basePath, string source, string destination) {
            PathSearchFilter searchFilter = PathResolver.ResolvePath(basePath, source);
            foreach (var file in Directory.EnumerateFiles(searchFilter.SearchDirectory,
                                                          searchFilter.SearchPattern,
                                                          searchFilter.SearchOption)) {
                var destinationPath = PathResolver.ResolvePackagePath(source, basePath, file, destination);
                using (Stream stream = File.OpenRead(file)) {
                    CreatePart(package, destinationPath, stream);
                }
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
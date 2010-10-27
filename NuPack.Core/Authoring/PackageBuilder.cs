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
            // Deserialize the document and extract the metadata
            Manifest manifest = Manifest.ReadFrom(stream);
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

        private void WriteManifest(Package package) {
            Uri uri = UriHelper.CreatePartUri(Id + Constants.ManifestExtension);

            // Create the manifest relationship
            package.CreateRelationship(uri, TargetMode.Internal, Constants.SchemaNamespace + ManifestRelationType);

            // Create the part
            PackagePart packagePart = package.CreatePart(uri, DefaultContentType, CompressionOption.Maximum);

            using (Stream stream = packagePart.GetStream()) {
                Manifest manifest = Manifest.Create(this);
                manifest.Save(stream);
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
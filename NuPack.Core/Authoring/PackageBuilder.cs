using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NuPack.Resources;
using Opc = System.IO.Packaging;

namespace NuPack {
    public class PackageBuilder {
        private const string DefaultContentType = "application/octet";
        private const string ReferenceRelationType = "Reference";
        private const string DependencyRelationType = "Dependency";
        private const string DependencyFileName = "Dependencies.xml";

        private readonly Dictionary<PackageFileType, List<IPackageFile>> _packageFiles;
        private readonly List<IPackageAssemblyReference> _references;
        private readonly List<PackageDependency> _dependencies;

        public PackageBuilder() {
            _packageFiles = new Dictionary<PackageFileType, List<IPackageFile>>();
            _dependencies = new List<PackageDependency>();
            _references = new List<IPackageAssemblyReference>();
            Keywords = new List<string>();
            Authors = new List<string>();

            foreach (int value in Enum.GetValues(typeof(PackageFileType))) {
                var key = (PackageFileType)value;
                _packageFiles[key] = new List<IPackageFile>();
            }
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

        public List<IPackageFile> PackageFiles {
            get {
                return _packageFiles[PackageFileType.Content];
            }
        }

        public List<IPackageFile> Resources {
            get {
                return _packageFiles[PackageFileType.Resources];
            }
        }

        public List<IPackageFile> Configuration {
            get {
                return _packageFiles[PackageFileType.Configuration];
            }
        }

        public List<PackageDependency> Dependencies {
            get {
                return _dependencies;
            }
        }

        public List<IPackageAssemblyReference> References {
            get {
                return _references;
            }
        }

        public List<IPackageFile> GetFiles(PackageFileType type) {
            return _packageFiles[type];
        }

        public void Save(Stream stream) {
            if (!IsValidBuild()) {
                throw new InvalidOperationException(NuPackResources.PackageBuilder_IdAndVersionRequired);
            }
            WritePackageContent(stream);
        }

        public bool IsValidBuild() {
            return !String.IsNullOrEmpty(Id) && (Version != null);
        }

        public static PackageBuilder ReadContentFrom(Package package) {
            PackageBuilder packageBuilder = new PackageBuilder();

            // Copy meta data
            packageBuilder.Authors.AddRange(package.Authors);
            packageBuilder.Category = package.Category;
            packageBuilder.Created = package.Created;
            packageBuilder.Description = package.Description;
            packageBuilder.Id = package.Id;
            packageBuilder.Keywords.AddRange(package.Keywords);
            packageBuilder.Version = package.Version;

            // Copy files
            packageBuilder.Dependencies.AddRange(package.Dependencies);
            packageBuilder.References.AddRange(package.AssemblyReferences);
            packageBuilder.Resources.AddRange(package.GetFiles(PackageFileType.Resources.ToString()));
            packageBuilder.PackageFiles.AddRange(package.GetFiles(PackageFileType.Content.ToString()));
            packageBuilder.Configuration.AddRange(package.GetFiles(PackageFileType.Configuration.ToString()));

            return packageBuilder;
        }

        public void WritePackageContent(Stream stream) {
            using (Opc.Package package = Opc.Package.Open(stream, FileMode.Create)) {

                WriteReferences(package);
                WriteFiles(package);
                WriteDepdendencies(package);

                // Copy the metadata properties back to the package
                package.PackageProperties.Category = Category;
                package.PackageProperties.Created = Created;
                package.PackageProperties.Creator = String.Join(", ", Authors);
                package.PackageProperties.Description = Description;
                package.PackageProperties.Identifier = Id;
                package.PackageProperties.Version = Version.ToString();
            }
        }

        private void WriteDepdendencies(Opc.Package package) {
            XDocument doc = new XDocument(new XElement("Dependencies"));
            foreach (var item in Dependencies) {
                var element = new XElement("Dependency", new XAttribute("id", item.Id));
                if (item.Version != null) {
                    element.Add(new XAttribute("Version", item.Version));
                }
                if (item.MinVersion != null) {
                    element.Add(new XAttribute("MinVersion", item.MinVersion));
                }
                if (item.MaxVersion != null) {
                    element.Add(new XAttribute("MaxVersion", item.MaxVersion));
                }
                doc.Root.Add(element);
            }
            
            Uri uri = UriHelper.CreatePartUri(DependencyFileName);

            // Create the relationship type
            package.CreateRelationship(uri, Opc.TargetMode.Internal, Package.SchemaNamespace + DependencyRelationType);

            // Create the part
            Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);

            using (Stream outStream = packagePart.GetStream()) {
                doc.Save(outStream);
            }

        }

        private void WriteReferences(Opc.Package package) {
            foreach (var referenceFile in References) {
                using (Stream readStream = referenceFile.Open()) {
                    var version = referenceFile.TargetFramework == null ? String.Empty : referenceFile.TargetFramework.ToString();
                    string path = Path.Combine("assemblies", version, Path.GetFileName(referenceFile.Path));
                    Uri uri = UriHelper.CreatePartUri(path);
                    Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);
                    using (Stream outStream = packagePart.GetStream()) {
                        readStream.CopyTo(outStream);
                    }
                    package.CreateRelationship(uri, Opc.TargetMode.Internal, Package.SchemaNamespace + ReferenceRelationType);
                }
            }
        }

        private void WriteFiles(Opc.Package package) {
            foreach (var fileTypeValue in Enum.GetValues(typeof(PackageFileType))) {
                var fileType = (PackageFileType)fileTypeValue;
                foreach (var packageFile in _packageFiles[fileType]) {
                    using (Stream stream = packageFile.Open()) {
                        CreatePart(package, fileType, packageFile.Path, stream);
                    }
                }
            }
        }

        private void CreatePart(Opc.Package package, PackageFileType fileType, string packagePath, Stream sourceStream) {
            Uri uri = UriHelper.CreatePartUri(packagePath);

            // Create the relationship type
            package.CreateRelationship(uri, Opc.TargetMode.Internal, GetPackageRelationName(fileType));

            // Create the part
            Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);

            using (Stream outStream = packagePart.GetStream()) {
                sourceStream.CopyTo(outStream);
            }
        }

        private static string GetPackageRelationName(PackageFileType fileType) {
            return Package.SchemaNamespace + fileType.ToString();
        }
    }
}

namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using NuPack.Resources;
    using Opc = System.IO.Packaging;

    internal class ZipPackage : Package {
        // REVIEW: Should we preserve the content type?
        private const string DefaultContentType = "application/octet";        
        private const string ConfigurationRelationshipType = "configuration";
        private const string DependenciesRelationshipType = "dependencies";
        private const string ReferencesRelationshipType = "reference";

        private readonly static char[] _delimiterChars = new[] { ';' };

        private Version _version;
        private string _id;
        private string _description;
        private IEnumerable<string> _authors;
        private string _category;
        private DateTime? _created;
        private DateTime? _modified;
        private string _lastModifiedBy;
        private IEnumerable<string> _keywords;
        private string _language;

        private IEnumerable<IPackageAssemblyReference> _references;
        private IEnumerable<PackageDependency> _dependencies;

        private ILookup<string, IPackageFile> _filesLookup;

        public ZipPackage(Stream stream) {
            var package = Opc.Package.Open(stream);
            VerifyPackage(package);
            ReadContents(package);
        }

        private void ReadContents(Opc.Package package) {
            // Get the metadata properties
            _id = package.PackageProperties.Identifier;
            _description = package.PackageProperties.Description;
            _authors = package.PackageProperties.Creator == null ? Enumerable.Empty<string>() : package.PackageProperties.Creator.Split(_delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            _category = package.PackageProperties.Category;
            _created = package.PackageProperties.Created ?? DateTime.Now;
            _version = Version.Parse(package.PackageProperties.Version);
            _language = package.PackageProperties.Language;
            _lastModifiedBy = package.PackageProperties.LastModifiedBy;
            _modified = package.PackageProperties.Modified ?? _created;
            _keywords = package.PackageProperties.Keywords == null ? Enumerable.Empty<string>() : package.PackageProperties.Keywords.Split(_delimiterChars, StringSplitOptions.RemoveEmptyEntries);

            // Each file that we care about in the package has a relationship with our prefix
            // We want to get a lookup from container name to list of files
            var packageFiles = from relationship in package.GetRelationships()
                               where relationship.RelationshipType.StartsWith(Package.SchemaNamespace, StringComparison.OrdinalIgnoreCase)
                               select new {
                                   Container = relationship.RelationshipType.Substring(Package.SchemaNamespace.Length),
                                   Part = package.GetPart(relationship.TargetUri)
                               };

            // Create a lookup from container name to parts
            var packageLookup = packageFiles.ToLookup(p => p.Container,
                                                      p => p.Part,
                                                      StringComparer.OrdinalIgnoreCase);

            _references = (from part in packageLookup[ReferencesRelationshipType]
                           select new PackageAssemblyReference(part)).ToList();

            Opc.PackagePart dependencyPart = packageLookup[DependenciesRelationshipType].SingleOrDefault();
            if (dependencyPart != null) {
                ProcessDependencies(dependencyPart);
            }
            else {
                _dependencies = Enumerable.Empty<PackageDependency>();
            }

            _filesLookup = packageFiles.ToLookup(p => p.Container,
                                                 p => new PackageFile(p.Part) as IPackageFile,
                                                 StringComparer.OrdinalIgnoreCase);
        }

        private static void VerifyPackage(Opc.Package package) {
            // Throw if one of the required properties is missing
            if (String.IsNullOrEmpty(package.PackageProperties.Identifier)) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, NuPackResources.PackageMissingRequiredProperty, "Identifier"));
            }

            if (String.IsNullOrEmpty(package.PackageProperties.Version)) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, NuPackResources.PackageMissingRequiredProperty, "Version"));
            }
        }

        private void ProcessDependencies(Opc.PackagePart dependencyPart) {
            using (Stream stream = dependencyPart.GetStream()) {
                _dependencies = ParseDependencies(XDocument.Load(stream).Root);
            }
        }

        private static IEnumerable<PackageDependency> ParseDependencies(XElement element) {
            // Get the list of dependencies for this package
            return from e in element.Elements("Dependency")
                   let minVersion = e.GetOptionalAttributeValue("MinVersion")
                   let maxVersion = e.GetOptionalAttributeValue("MaxVersion")
                   let version = e.GetOptionalAttributeValue("Version")
                   select PackageDependency.CreateDependency(e.Attribute("Id").Value,
                                                             Utility.ParseOptionalVersion(minVersion),
                                                             Utility.ParseOptionalVersion(maxVersion),
                                                             Utility.ParseOptionalVersion(version));
        }

        public override IEnumerable<IPackageFile> GetFiles(string fileType) {
            return _filesLookup.Contains(fileType) ? _filesLookup[fileType] : Enumerable.Empty<IPackageFile>();
        }

        public override DateTime Created {
            get {
                return _created.Value;
            }
        }

        public override IEnumerable<string> Authors {
            get {
                return _authors;
            }
        }

        public override string Category {
            get {
                return _category;
            }
        }

        public override string Id {
            get {
                return _id;
            }
        }

        public override Version Version {
            get {
                return _version;
            }
        }

        public override string Description {
            get {
                return _description;
            }
        }

        public override IEnumerable<string> Keywords {
            get {
                return _keywords;
            }
        }

        public override string Language {
            get {
                return _language;
            }
        }

        public override DateTime Modified {
            get {
                return _modified.Value;
            }
        }

        public override string LastModifiedBy {
            get {
                return _lastModifiedBy;
            }
        }

        public override IEnumerable<PackageDependency> Dependencies {
            get {
                return _dependencies;
            }
        }

        public override IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                return _references;
            }
        }

        public override void Save(Stream stream) {
            using (var package = Opc.Package.Open(stream, FileMode.Create)) {
                foreach (var fileGroup in _filesLookup) {
                    CreateFiles(package, Package.SchemaNamespace + fileGroup.Key, fileGroup);
                }

                // Copy the metadata properties back to the package
                package.PackageProperties.Category = Category;
                package.PackageProperties.Created = Created;
                package.PackageProperties.Creator = String.Join(";", Authors);
                package.PackageProperties.Description = Description;
                package.PackageProperties.Identifier = Id;
                package.PackageProperties.Version = Version.ToString();
                package.PackageProperties.Keywords = String.Join(";", Keywords);
                package.PackageProperties.Language = Language;
                package.PackageProperties.LastModifiedBy = LastModifiedBy;
                package.PackageProperties.Modified = Modified;                
            }
        }

        private static void CreateFiles(Opc.Package package, string relationshipType, IEnumerable<IPackageFile> files) {
            foreach (IPackageFile file in files) {
                CreateFile(package, relationshipType, file);
            }
        }

        private static void CreateFile(Opc.Package package, string relationshipType, IPackageFile file) {
            // Get the stream
            using (Stream readStream = file.Open()) {
                Opc.PackagePart part = CreatePart(package, relationshipType, file.Path);
                using (Stream outStream = part.GetStream()) {
                    readStream.CopyTo(outStream);
                }
            }
        }

        private static Opc.PackagePart CreatePart(Opc.Package package, string relationshipType, string path) {
            Uri uri = UriHelper.CreatePartUri(path);
            // Create the relationship type
            package.CreateRelationship(uri, Opc.TargetMode.Internal, relationshipType);
            // Create the part
            return package.CreatePart(uri, DefaultContentType);
        }
    }
}
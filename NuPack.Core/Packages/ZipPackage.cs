namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NuPack.Resources;
    using Opc = System.IO.Packaging;

    public class ZipPackage : Package {
        private const string AssemblyReferencesDir = "lib";
        private const string AssemblyReferencesExtension = ".dll";

        // paths to exclude
        private static readonly string[] _excludePaths = new[] { "_rels", "package" };

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

        private IEnumerable<IPackageFile> _files;

        public ZipPackage(Stream stream) {
            var package = Opc.Package.Open(stream);
            ReadContents(package);
        }

        private void ReadContents(Opc.Package package) {
            // REVIEW: Should we include the manifest?

            _files = (from part in package.GetParts()
                      where IsPackageFile(part)
                      select new PackageFile(part)).ToList();

            _references = (from part in package.GetParts()                           
                           where IsAssemblyReference(part)
                           select new PackageAssemblyReference(part)).ToList();

            var relationshipType = package.GetRelationshipsByType(Package.SchemaNamespace + PackageBuilder.ManifestRelationType).SingleOrDefault();

            if (relationshipType == null) {
                throw new InvalidOperationException(NuPackResources.PackageDoesNotContainManifest);
            }

            Opc.PackagePart specPart = package.GetPart(relationshipType.TargetUri);

            if (specPart == null) {
                throw new InvalidOperationException(NuPackResources.PackageDoesNotContainManifest);
            }

            using (Stream stream = specPart.GetStream()) {
                PackageBuilder builder = PackageBuilder.ReadFrom(stream);

                // Get the metadata properties
                _id = builder.Id;
                _description = builder.Description;
                _authors = builder.Authors;
                _category = builder.Category;
                _created = builder.Created;
                _version = builder.Version;
                _language = builder.Language;
                _lastModifiedBy = builder.LastModifiedBy;
                _modified = builder.Modified;
                _keywords = builder.Keywords;

                // Get dependencies
                _dependencies = builder.Dependencies;
            }
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

        public override IEnumerable<IPackageFile> GetFiles() {
            return _files;
        }
        
        private static bool IsAssemblyReference(Opc.PackagePart part) {
            // Assembly references are in lib/ and have a .dll extension
            var path = UriHelper.GetPath(part.Uri);
            return path.StartsWith(AssemblyReferencesDir, StringComparison.OrdinalIgnoreCase) &&
                   Path.GetExtension(path).Equals(AssemblyReferencesExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPackageFile(Opc.PackagePart part) {
            string path = UriHelper.GetPath(part.Uri);
            // We exclude any opc files and the manifest file (.nuspec)
            return !_excludePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                   !Utility.IsManifest(path);
        }
    }
}
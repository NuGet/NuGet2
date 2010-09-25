namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NuPack.Resources;
    using Opc = System.IO.Packaging;

    public class ZipPackage : IPackage {
        private const string AssemblyReferencesDir = "lib";
        private const string AssemblyReferencesExtension = ".dll";

        // paths to exclude
        private static readonly string[] _excludePaths = new[] { "_rels", "package" };

        private PackageBuilder _packageBuilder;

        private IEnumerable<IPackageAssemblyReference> _references;
        private IEnumerable<IPackageFile> _files;


        public ZipPackage(Stream stream) {
            var package = Opc.Package.Open(stream);
            ReadContents(package);
        }

        private void ReadContents(Opc.Package package) {
            // REVIEW: Should we include the manifest?

            _files = (from part in package.GetParts()
                      where IsPackageFile(part)
                      select new ZipPackageFile(part)).ToList();

            _references = (from part in package.GetParts()
                           where IsAssemblyReference(part)
                           select new ZipPackageAssemblyReference(part)).ToList();

            var relationshipType = package.GetRelationshipsByType(Constants.SchemaNamespace + PackageBuilder.ManifestRelationType).SingleOrDefault();

            if (relationshipType == null) {
                throw new InvalidOperationException(NuPackResources.PackageDoesNotContainManifest);
            }

            Opc.PackagePart specPart = package.GetPart(relationshipType.TargetUri);

            if (specPart == null) {
                throw new InvalidOperationException(NuPackResources.PackageDoesNotContainManifest);
            }

            using (Stream stream = specPart.GetStream()) {
                // Get the metadata properties
                _packageBuilder = PackageBuilder.ReadFrom(stream);
            }
        }

        public DateTime Created {
            get {
                return _packageBuilder.Created;
            }
        }

        public IEnumerable<string> Authors {
            get {
                return _packageBuilder.Authors;
            }
        }

        public string Category {
            get {
                return _packageBuilder.Category;
            }
        }

        public string Id {
            get {
                return _packageBuilder.Id;
            }
        }

        public bool RequireLicenseAcceptance {
            get {
                return _packageBuilder.RequireLicenseAcceptance;
            }
        }

        public Uri LicenseUrl {
            get {
                return _packageBuilder.LicenseUrl;
            }
        }

        public Version Version {
            get {
                return _packageBuilder.Version;
            }
        }

        public string Description {
            get {
                return _packageBuilder.Description;
            }
        }

        public IEnumerable<string> Keywords {
            get {
                return _packageBuilder.Keywords;
            }
        }

        public string Language {
            get {
                return _packageBuilder.Language;
            }
        }

        public DateTime Modified {
            get {
                return _packageBuilder.Modified;
            }
        }

        public string LastModifiedBy {
            get {
                return _packageBuilder.LastModifiedBy;
            }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get {
                return _packageBuilder.Dependencies;
            }
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                return _references;
            }
        }

        public IEnumerable<IPackageFile> GetFiles() {
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
namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Packaging;
    using System.Linq;
    using Microsoft.Internal.Web.Utils;
    using NuPack.Resources;

    public class ZipPackage : IPackage {
        private const string AssemblyReferencesDir = "lib";
        private const string AssemblyReferencesExtension = ".dll";

        // paths to exclude
        private static readonly string[] _excludePaths = new[] { "_rels", "package" };

        private PackageBuilder _metadata;
        // We don't store the steam itself, just a way to open the stream on demand
        // so we don't have to hold on to that resource
        private Func<Stream> _streamFactory;

        public ZipPackage(string fileName) {
            if (String.IsNullOrEmpty(fileName)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "fileName");
            }
            _streamFactory = () => File.OpenRead(fileName);
        }

        public ZipPackage(Func<Stream> streamFactory) {
            if (streamFactory == null) {
                throw new ArgumentNullException("streamFactory");
            }
            _streamFactory = streamFactory;
        }

        private PackageBuilder Metadata {
            get {
                if (_metadata == null) {
                    _metadata = GetMetadata();
                }
                return _metadata;
            }
        }

        public DateTime Created {
            get {
                return Metadata.Created;
            }
        }

        public IEnumerable<string> Authors {
            get {
                return Metadata.Authors;
            }
        }

        public string Category {
            get {
                return Metadata.Category;
            }
        }

        public string Id {
            get {
                return Metadata.Id;
            }
        }

        public bool RequireLicenseAcceptance {
            get {
                return Metadata.RequireLicenseAcceptance;
            }
        }

        public Uri LicenseUrl {
            get {
                return Metadata.LicenseUrl;
            }
        }

        public Version Version {
            get {
                return Metadata.Version;
            }
        }

        public string Description {
            get {
                return Metadata.Description;
            }
        }

        public IEnumerable<string> Keywords {
            get {
                return Metadata.Keywords;
            }
        }

        public string Language {
            get {
                return Metadata.Language;
            }
        }

        public DateTime Modified {
            get {
                return Metadata.Modified;
            }
        }

        public string LastModifiedBy {
            get {
                return Metadata.LastModifiedBy;
            }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get {
                return Metadata.Dependencies;
            }
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                using (Stream stream = _streamFactory()) {
                    Package package = Package.Open(stream);
                    return (from part in package.GetParts()
                            where IsAssemblyReference(part)
                            select new ZipPackageAssemblyReference(part)).ToList();
                }
            }
        }

        public IEnumerable<IPackageFile> GetFiles() {
            using (Stream stream = _streamFactory()) {
                Package package = Package.Open(stream);

                return (from part in package.GetParts()
                        where IsPackageFile(part)
                        select new ZipPackageFile(part)).ToList();
            }
        }

        private PackageBuilder GetMetadata() {
            using (Stream stream = _streamFactory()) {
                Package package = Package.Open(stream);

                PackageRelationship relationshipType = package.GetRelationshipsByType(Constants.SchemaNamespace + PackageBuilder.ManifestRelationType).SingleOrDefault();

                if (relationshipType == null) {
                    throw new InvalidOperationException(NuPackResources.PackageDoesNotContainManifest);
                }

                PackagePart manifest = package.GetPart(relationshipType.TargetUri);

                if (manifest == null) {
                    throw new InvalidOperationException(NuPackResources.PackageDoesNotContainManifest);
                }

                using (Stream manifestStream = manifest.GetStream()) {
                    return PackageBuilder.ReadFrom(manifestStream);
                }
            }
        }

        private static bool IsAssemblyReference(PackagePart part) {
            // Assembly references are in lib/ and have a .dll extension
            var path = UriHelper.GetPath(part.Uri);
            return path.StartsWith(AssemblyReferencesDir, StringComparison.OrdinalIgnoreCase) &&
                   Path.GetExtension(path).Equals(AssemblyReferencesExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPackageFile(PackagePart part) {
            string path = UriHelper.GetPath(part.Uri);
            // We exclude any opc files and the manifest file (.nuspec)
            return !_excludePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                   !Utility.IsManifest(path);
        }
    }
}
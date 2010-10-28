using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using Microsoft.Internal.Web.Utils;
using NuGet.Resources;

namespace NuGet {
    public class ZipPackage : IPackage {
        private const string AssemblyReferencesDir = "lib";
        private const string AssemblyReferencesExtension = ".dll";

        // paths to exclude
        private static readonly string[] _excludePaths = new[] { "_rels", "package" };

        // We don't store the steam itself, just a way to open the stream on demand
        // so we don't have to hold on to that resource
        private Func<Stream> _streamFactory;

        public ZipPackage(string fileName) {
            if (String.IsNullOrEmpty(fileName)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "fileName");
            }
            _streamFactory = () => File.OpenRead(fileName);
            EnsureManifest();
        }

        internal ZipPackage(Func<Stream> streamFactory) {
            if (streamFactory == null) {
                throw new ArgumentNullException("streamFactory");
            }
            _streamFactory = streamFactory;
            EnsureManifest();
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

        public IEnumerable<string> Authors {
            get;
            set;
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

        public IEnumerable<PackageDependency> Dependencies {
            get;
            set;
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

        public Stream GetStream() {
            return _streamFactory();
        }

        private void EnsureManifest() {
            using (Stream stream = _streamFactory()) {
                Package package = Package.Open(stream);

                PackageRelationship relationshipType = package.GetRelationshipsByType(Constants.SchemaNamespace + PackageBuilder.ManifestRelationType).SingleOrDefault();

                if (relationshipType == null) {
                    throw new InvalidOperationException(NuGetResources.PackageDoesNotContainManifest);
                }

                PackagePart manifestPart = package.GetPart(relationshipType.TargetUri);

                if (manifestPart == null) {
                    throw new InvalidOperationException(NuGetResources.PackageDoesNotContainManifest);
                }

                using (Stream manifestStream = manifestPart.GetStream()) {
                    Manifest manifest = Manifest.ReadFrom(manifestStream);
                    IPackageMetadata metadata = manifest.Metadata;

                    Id = metadata.Id;
                    Version = metadata.Version;
                    Title = metadata.Title;
                    Authors = metadata.Authors;
                    IconUrl = metadata.IconUrl;
                    LicenseUrl = metadata.LicenseUrl;
                    ProjectUrl = metadata.ProjectUrl;
                    RequireLicenseAcceptance = metadata.RequireLicenseAcceptance;
                    Description = metadata.Description;
                    Summary = metadata.Summary;
                    Language = metadata.Language;
                    Dependencies = metadata.Dependencies;
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

        public override string ToString() {
            return this.GetFullName();
        }
    }
}

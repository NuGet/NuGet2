using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.Caching;
using Microsoft.Internal.Web.Utils;
using NuGet.Resources;

namespace NuGet {
    public class ZipPackage : IPackage {
        private const string AssemblyReferencesDir = "lib";
        private const string ResourceAssemblyExtension = ".resources.dll";
        private const string CacheKeyFormat = "NUGET_ZIP_PACKAGE_{0}_{1}{2}";
        private const string AssembliesCacheKey = "ASSEMBLIES";
        private const string FilesCacheKey = "FILES";

        private readonly bool _enableCaching;

        private static readonly string[] AssemblyReferencesExtensions = new[] { ".dll", ".exe" };

        private static readonly TimeSpan CacheTimeout = TimeSpan.FromSeconds(15);

        // paths to exclude
        private static readonly string[] _excludePaths = new[] { "_rels", "package" };

        // We don't store the steam itself, just a way to open the stream on demand
        // so we don't have to hold on to that resource
        private Func<Stream> _streamFactory;

        public ZipPackage(string fileName)
            : this(fileName, enableCaching: false) {
        }

        public ZipPackage(Stream stream) {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }
            _enableCaching = false;
            _streamFactory = stream.ToStreamFactory();
            EnsureManifest();
        }

        internal ZipPackage(string fileName, bool enableCaching) {
            if (String.IsNullOrEmpty(fileName)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "fileName");
            }
            _enableCaching = enableCaching;
            _streamFactory = () => File.OpenRead(fileName);
            EnsureManifest();
        }

        internal ZipPackage(Func<Stream> streamFactory) {
            if (streamFactory == null) {
                throw new ArgumentNullException("streamFactory");
            }
            _enableCaching = true;
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

        public IEnumerable<string> Owners {
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

        public Uri ReportAbuseUrl {
            get {
                return null;
            }
        }

        public int DownloadCount {
            get {
                return -1;
            }
        }

        public double Rating {
            get {
                return -1;
            }
        }

        public int RatingsCount {
            get {
                return 0;
            }
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

        public string Tags {
            get;
            set;
        }

        public IEnumerable<PackageDependency> Dependencies {
            get;
            set;
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                if (_enableCaching) {
                    return MemoryCache.Default.GetOrAdd(GetAssembliesCacheKey(), GetAssembliesNoCache, CacheTimeout);
                }
                return GetAssembliesNoCache();
            }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies {
            get;
            set;
        }

        public IEnumerable<IPackageFile> GetFiles() {
            if (_enableCaching) {
                return MemoryCache.Default.GetOrAdd(GetFilesCacheKey(), GetFilesNoCache, CacheTimeout);
            }
            return GetFilesNoCache();
        }

        public Stream GetStream() {
            return _streamFactory();
        }

        private List<IPackageAssemblyReference> GetAssembliesNoCache() {
            return (from file in GetFiles()
                    where IsAssemblyReference(file)
                    select (IPackageAssemblyReference)new ZipPackageAssemblyReference(file)).ToList();
        }

        private List<IPackageFile> GetFilesNoCache() {
            using (Stream stream = _streamFactory()) {
                Package package = Package.Open(stream);

                return (from part in package.GetParts()
                        where IsPackageFile(part)
                        select (IPackageFile)new ZipPackageFile(part)).ToList();
            }
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
                    Owners = metadata.Owners;
                    IconUrl = metadata.IconUrl;
                    LicenseUrl = metadata.LicenseUrl;
                    ProjectUrl = metadata.ProjectUrl;
                    RequireLicenseAcceptance = metadata.RequireLicenseAcceptance;
                    Description = metadata.Description;
                    Summary = metadata.Summary;
                    Language = metadata.Language;
                    Tags = metadata.Tags;
                    Dependencies = metadata.Dependencies;
                    FrameworkAssemblies = metadata.FrameworkAssemblies;

                    // Ensure tags start and end with an empty " " so we can do contains filtering reliably
                    if (!String.IsNullOrEmpty(Tags)) {
                        Tags = " " + Tags + " ";
                    }
                }
            }
        }

        private static bool IsAssemblyReference(IPackageFile file) {
            // Assembly references are in lib/ and have a .dll/.exe extension
            var path = file.Path;
            return path.StartsWith(AssemblyReferencesDir, StringComparison.OrdinalIgnoreCase) &&
                // Exclude resource assemblies
                   !path.EndsWith(ResourceAssemblyExtension, StringComparison.OrdinalIgnoreCase) &&
                   AssemblyReferencesExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsPackageFile(PackagePart part) {
            string path = UriUtility.GetPath(part.Uri);
            // We exclude any opc files and the manifest file (.nuspec)
            return !_excludePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                   !PackageUtility.IsManifest(path);
        }

        public override string ToString() {
            return this.GetFullName();
        }

        private string GetFilesCacheKey() {
            return String.Format(CultureInfo.InvariantCulture, CacheKeyFormat, FilesCacheKey, Id, Version);
        }

        private string GetAssembliesCacheKey() {
            return String.Format(CultureInfo.InvariantCulture, CacheKeyFormat, AssembliesCacheKey, Id, Version);
        }

        internal static void ClearCache(IPackage package) {
            var zipPackage = package as ZipPackage;

            // Remove the cache entries for files and assemblies
            if (zipPackage != null) {
                MemoryCache.Default.Remove(zipPackage.GetAssembliesCacheKey());
                MemoryCache.Default.Remove(zipPackage.GetFilesCacheKey());
            }
        }
    }
}

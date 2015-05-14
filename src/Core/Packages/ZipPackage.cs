using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Resources;

namespace NuGet
{
    public class ZipPackage : LocalPackage
    {
        private const string CacheKeyFormat = "NUGET_ZIP_PACKAGE_{0}_{1}{2}";
        private const string AssembliesCacheKey = "ASSEMBLIES";
        private const string FilesCacheKey = "FILES";

        private readonly bool _enableCaching;

        private static readonly TimeSpan CacheTimeout = TimeSpan.FromSeconds(15);

        // paths to exclude
        private static readonly string[] ExcludePaths = new[] { "_rels", "package" };

        // We don't store the stream itself, just a way to open the stream on demand
        // so we don't have to hold on to that resource
        private readonly Func<Stream> _streamFactory;

        public ZipPackage(string filePath)
            : this(filePath, enableCaching: false)
        {
        }

        public ZipPackage(Func<Stream> packageStreamFactory, Func<Stream> manifestStreamFactory)
        {
            if (packageStreamFactory == null)
            {
                throw new ArgumentNullException("packageStreamFactory");
            }

            if (manifestStreamFactory == null)
            {
                throw new ArgumentNullException("manifestStreamFactory");
            }

            _enableCaching = false;
            _streamFactory = packageStreamFactory;
            EnsureManifest(manifestStreamFactory);
        }

        public ZipPackage(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            _enableCaching = false;
            _streamFactory = stream.ToStreamFactory();
            using (stream = _streamFactory())
            {
                EnsureManifest(() => GetManifestStreamFromPackage(stream));
            }
        }

        private ZipPackage(string filePath, bool enableCaching)
        {
            if (String.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "filePath");
            }
            _enableCaching = enableCaching;
            _streamFactory = () => File.OpenRead(filePath);
            using (var stream = _streamFactory())
            {
                EnsureManifest(() => GetManifestStreamFromPackage(stream));
            }
        }

        internal ZipPackage(Func<Stream> streamFactory, bool enableCaching)
        {
            if (streamFactory == null)
            {
                throw new ArgumentNullException("streamFactory");
            }
            _enableCaching = enableCaching;
            _streamFactory = streamFactory;
            using (var stream = _streamFactory())
            {
                EnsureManifest(() => GetManifestStreamFromPackage(stream));
            }
        }

        public override Stream GetStream()
        {
            return _streamFactory();
        }

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            IEnumerable<FrameworkName> fileFrameworks;
            IEnumerable<IPackageFile> cachedFiles;
            if (_enableCaching && MemoryCache.Instance.TryGetValue(GetFilesCacheKey(), out cachedFiles))
            {
                fileFrameworks = cachedFiles.Select(c => c.TargetFramework);
            }
            else
            {
                using (Stream stream = _streamFactory())
                {
                    var package = Package.Open(stream);

                    string effectivePath;
                    fileFrameworks = from part in package.GetParts()
                                     where IsPackageFile(part)
                                     select VersionUtility.ParseFrameworkNameFromFilePath(UriUtility.GetPath(part.Uri), out effectivePath);

                }
            }

            return base.GetSupportedFrameworks()
                       .Concat(fileFrameworks)
                       .Where(f => f != null)
                       .Distinct();
        }

        protected override IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesCore()
        {
            if (_enableCaching)
            {
                return MemoryCache.Instance.GetOrAdd(GetAssembliesCacheKey(), GetAssembliesNoCache, CacheTimeout);
            }

            return GetAssembliesNoCache();
        }

        protected override IEnumerable<IPackageFile> GetFilesBase()
        {
            if (_enableCaching)
            {
                return MemoryCache.Instance.GetOrAdd(GetFilesCacheKey(), GetFilesNoCache, CacheTimeout);
            }
            return GetFilesNoCache();
        }

        private List<IPackageAssemblyReference> GetAssembliesNoCache()
        {
            return (from file in GetFiles()
                    where IsAssemblyReference(file.Path)
                    select (IPackageAssemblyReference)new ZipPackageAssemblyReference(file)).ToList();
        }

        private List<IPackageFile> GetFilesNoCache()
        {
            using (Stream stream = _streamFactory())
            {
                Package package = Package.Open(stream);

                return (from part in package.GetParts()
                        where IsPackageFile(part)
                        select (IPackageFile)new ZipPackageFile(part)).ToList();
            }
        }

        private void EnsureManifest(Func<Stream> manifestStreamFactory)
        {
            using (Stream manifestStream = manifestStreamFactory())
            {
                ReadManifest(manifestStream);
            }
        }

        private static Stream GetManifestStreamFromPackage(Stream packageStream)
        {
            Package package = Package.Open(packageStream);

            PackageRelationship relationshipType = package.GetRelationshipsByType(Constants.PackageRelationshipNamespace + PackageBuilder.ManifestRelationType).SingleOrDefault();

            if (relationshipType == null)
            {
                throw new InvalidOperationException(NuGetResources.PackageDoesNotContainManifest);
            }

            PackagePart manifestPart = package.GetPart(relationshipType.TargetUri);

            if (manifestPart == null)
            {
                throw new InvalidOperationException(NuGetResources.PackageDoesNotContainManifest);
            }

            return manifestPart.GetStream();
        }

        private string GetFilesCacheKey()
        {
            return String.Format(CultureInfo.InvariantCulture, CacheKeyFormat, FilesCacheKey, Id, Version);
        }

        private string GetAssembliesCacheKey()
        {
            return String.Format(CultureInfo.InvariantCulture, CacheKeyFormat, AssembliesCacheKey, Id, Version);
        }

        internal static bool IsPackageFile(PackagePart part)
        {
            string path = UriUtility.GetPath(part.Uri);
            string directory = Path.GetDirectoryName(path);

            // We exclude any opc files and the manifest file (.nuspec)
            return !ExcludePaths.Any(p => directory.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                   !PackageHelper.IsManifest(path);
        }

        internal static void ClearCache(IPackage package)
        {
            var zipPackage = package as ZipPackage;

            // Remove the cache entries for files and assemblies
            if (zipPackage != null)
            {
                MemoryCache.Instance.Remove(zipPackage.GetAssembliesCacheKey());
                MemoryCache.Instance.Remove(zipPackage.GetFilesCacheKey());
            }
        }
    }
}
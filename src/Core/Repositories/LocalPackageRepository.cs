using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet {
    public class LocalPackageRepository : PackageRepositoryBase, IPackageLookup {
        private Dictionary<string, PackageCacheEntry> _packageCache = new Dictionary<string, PackageCacheEntry>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<PackageName, string> _packagePathLookup = new Dictionary<PackageName, string>();
        private readonly bool _enableCaching;

        public LocalPackageRepository(string physicalPath)
            : this(physicalPath, enableCaching: true) {
        }

        public LocalPackageRepository(string physicalPath, bool enableCaching)
            : this(new DefaultPackagePathResolver(physicalPath),
                   new PhysicalFileSystem(physicalPath),
                   enableCaching) {
        }

        public LocalPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem)
            : this(pathResolver, fileSystem, enableCaching: true) {
        }

        public LocalPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem, bool enableCaching) {
            if (pathResolver == null) {
                throw new ArgumentNullException("pathResolver");
            }

            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }

            FileSystem = fileSystem;
            PathResolver = pathResolver;
            _enableCaching = enableCaching;
        }

        public override string Source {
            get {
                return FileSystem.Root;
            }
        }

        protected IFileSystem FileSystem {
            get;
            private set;
        }

        private IPackagePathResolver PathResolver {
            get;
            set;
        }

        public override IQueryable<IPackage> GetPackages() {
            return GetPackages(OpenPackage).AsSafeQueryable();
        }

        public override void AddPackage(IPackage package) {
            string packageFilePath = GetPackageFilePath(package);

            FileSystem.AddFileWithCheck(packageFilePath, package.GetStream);
        }

        public override void RemovePackage(IPackage package) {
            // Delete the package file
            string packageFilePath = GetPackageFilePath(package);
            FileSystem.DeleteFileSafe(packageFilePath);

            // Delete the package directory if any
            FileSystem.DeleteDirectorySafe(PathResolver.GetPackageDirectory(package), recursive: false);

            // If this is the last package delete the package directory
            if (!FileSystem.GetFilesSafe(String.Empty).Any() &&
                !FileSystem.GetDirectoriesSafe(String.Empty).Any()) {
                FileSystem.DeleteDirectorySafe(String.Empty, recursive: false);
            }
        }

        public IPackage FindPackage(string packageId, Version version) {
            return FindPackage(OpenPackage, packageId, version);
        }

        internal IPackage FindPackage(Func<string, IPackage> openPackage, string packageId, Version version) {
            return (from path in GetPackagePaths(packageId, version)
                    where FileSystem.FileExists(path)
                    let package = GetPackage(openPackage, path)
                    where package.Version == version
                    select package).FirstOrDefault();
        }

        private IEnumerable<string> GetPackagePaths(string packageId, Version version) {
            var packageName = new PackageName(packageId, version);
            string packagePath;
            if (_packagePathLookup.TryGetValue(packageName, out packagePath)) {
                yield return packagePath;
            }
            yield return GetPackageFilePath(packageId, version);
            yield return PathResolver.GetPackageFileName(packageId, version);
        }

        internal IEnumerable<IPackage> GetPackages(Func<string, IPackage> openPackage) {
            foreach (var path in GetPackageFiles()) {
                IPackage package = GetPackage(openPackage, path);

                yield return package;
            }
        }

        private IPackage GetPackage(Func<string, IPackage> openPackage, string path) {
            PackageCacheEntry cacheEntry;
            DateTimeOffset lastModified = FileSystem.GetLastModified(path);
            // If we never cached this file or we did and it's current last modified time is newer
            // create a new entry
            if (!_packageCache.TryGetValue(path, out cacheEntry) ||
                (cacheEntry != null && lastModified > cacheEntry.LastModifiedTime)) {
                // We need to do this so we capture the correct loop variable
                string packagePath = path;

                // Create the package
                IPackage package = openPackage(packagePath);


                // create a cache entry with the last modified time
                cacheEntry = new PackageCacheEntry(package, lastModified);

                if (_enableCaching) {
                    // Store the entry
                    _packageCache[packagePath] = cacheEntry;
                    _packagePathLookup[new PackageName(package.Id, package.Version)] = path;
                }
            }

            return cacheEntry.Package;
        }

        internal IEnumerable<string> GetPackageFiles() {
            // Check for package files one level deep. We use this at package install time
            // to determine the set of installed packages. Installed packages are copied to 
            // {id}.{version}\{packagefile}.{extension}.
            foreach (var dir in FileSystem.GetDirectories(String.Empty)) {
                foreach (var path in FileSystem.GetFiles(dir, "*" + Constants.PackageExtension)) {
                    yield return path;
                }
            }

            // Check top level directory
            foreach (var path in FileSystem.GetFiles(String.Empty, "*" + Constants.PackageExtension)) {
                yield return path;
            }
        }

        protected virtual IPackage OpenPackage(string path) {
            var package = new ZipPackage(() => FileSystem.OpenFile(path), _enableCaching);

            // Clear the cache whenever we open a new package file
            ZipPackage.ClearCache(package);
            return package;
        }

        protected virtual string GetPackageFilePath(IPackage package) {
            return Path.Combine(PathResolver.GetPackageDirectory(package),
                                PathResolver.GetPackageFileName(package));
        }

        protected virtual string GetPackageFilePath(string id, Version version) {
            return Path.Combine(PathResolver.GetPackageDirectory(id, version),
                                PathResolver.GetPackageFileName(id, version));
        }

        private class PackageCacheEntry {
            public PackageCacheEntry(IPackage package, DateTimeOffset lastModifiedTime) {
                Package = package;
                LastModifiedTime = lastModifiedTime;
            }

            public IPackage Package { get; private set; }
            public DateTimeOffset LastModifiedTime { get; private set; }
        }

        private class PackageName : IEquatable<PackageName> {
            public PackageName(string packageId, Version version) {
                PackageId = packageId;
                Version = version;
            }

            public string PackageId { get; private set; }
            public Version Version { get; private set; }

            public bool Equals(PackageName other) {
                return PackageId.Equals(other.PackageId, StringComparison.OrdinalIgnoreCase) &&
                       Version.Equals(other.Version);
            }

            public override int GetHashCode() {
                var combiner = new HashCodeCombiner();
                combiner.AddObject(PackageId);
                combiner.AddObject(Version);

                return combiner.CombinedHash;
            }

            public override string ToString() {
                return PackageId + " " + Version;
            }
        }
    }
}

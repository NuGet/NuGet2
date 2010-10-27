namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class LocalPackageRepository : PackageRepositoryBase {
        private Dictionary<string, PackageCacheEntry> _packageCache = new Dictionary<string, PackageCacheEntry>(StringComparer.OrdinalIgnoreCase);

        public LocalPackageRepository(string physicalPath)
            : this(new DefaultPackagePathResolver(physicalPath),
                   new FileBasedProjectSystem(physicalPath)) {
        }

        public LocalPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem) {
            if (pathResolver == null) {
                throw new ArgumentNullException("pathResolver");
            }

            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }

            FileSystem = fileSystem;
            PathResolver = pathResolver;
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
            return GetPackages(OpenPackage).AsQueryable();
        }

        internal IEnumerable<IPackage> GetPackages(Func<string, IPackage> openPackage) {
            foreach (var path in GetPackageFiles()) {
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

                    // Store the entry
                    _packageCache[packagePath] = cacheEntry;
                }

                yield return cacheEntry.Package;
            }
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

        private IPackage OpenPackage(string path) {
            return new ZipPackage(() => FileSystem.OpenFile(path));
        }

        private string GetPackageFilePath(IPackage package) {
            return Path.Combine(PathResolver.GetPackageDirectory(package),
                                PathResolver.GetPackageFileName(package));
        }

        private class PackageCacheEntry {
            public PackageCacheEntry(IPackage package, DateTimeOffset lastModifiedTime) {
                Package = package;
                LastModifiedTime = lastModifiedTime;
            }

            public IPackage Package { get; private set; }
            public DateTimeOffset LastModifiedTime { get; private set; }
        }
    }
}

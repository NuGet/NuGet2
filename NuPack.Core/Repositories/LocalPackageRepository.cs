namespace NuPack {
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
            var packages = new List<IPackage>();
            foreach (var path in GetPackageFiles()) {
                try {
                    PackageCacheEntry cacheEntry;
                    DateTime lastModified = FileSystem.GetLastModified(path);
                    // If we never cached this file or we did and it's current last modified time is newer
                    // create a new entry
                    if (!_packageCache.TryGetValue(path, out cacheEntry) ||
                        (cacheEntry != null && lastModified > cacheEntry.LastModifiedTime)) {
                        // We need to do this so we capture the correct loop variable
                        string packagePath = path;

                        // Create the package
                        var package = new ZipPackage(() => FileSystem.OpenFile(packagePath));

                        // create a cache entry with the last modified time
                        cacheEntry = new PackageCacheEntry(package, lastModified);

                        // Store the entry
                        _packageCache[packagePath] = cacheEntry;
                    }

                    packages.Add(cacheEntry.Package);
                }
                catch (NotSupportedException) {
                    // If this is an unsupported package then skip it
                }
            }
            return packages.AsQueryable();
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

            FileSystem.AddFile(packageFilePath, stream => PackageBuilder.Save(package, stream));
        }

        public override void RemovePackage(IPackage package) {
            // Delete the package file
            string packageFilePath = GetPackageFilePath(package);
            FileSystem.DeleteFile(packageFilePath);

            // Delete the package directory if any
            FileSystem.DeleteDirectory(PathResolver.GetPackageDirectory(package), true);

            // If this is the last package delete the package directory
            if (!FileSystem.GetFiles(String.Empty).Any() &&
                !FileSystem.GetDirectories(String.Empty).Any()) {
                FileSystem.DeleteDirectory(String.Empty, recursive: false);
            }
        }

        private string GetPackageFilePath(IPackage package) {
            return Path.Combine(PathResolver.GetPackageDirectory(package),
                                PathResolver.GetPackageFileName(package));
        }

        private class PackageCacheEntry {
            public PackageCacheEntry(IPackage package, DateTime lastModifiedTime) {
                Package = package;
                LastModifiedTime = lastModifiedTime;
            }

            public IPackage Package { get; private set; }
            public DateTime LastModifiedTime { get; private set; }
        }
    }
}

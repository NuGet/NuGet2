namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class LocalPackageRepository : PackageRepositoryBase {
        private Dictionary<string, IPackage> _packageCache = new Dictionary<string, IPackage>(StringComparer.OrdinalIgnoreCase);

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
                    IPackage package;
                    if (!_packageCache.TryGetValue(path, out package)) {
                        using (Stream stream = FileSystem.OpenFile(path)) {
                            package = new ZipPackage(stream);
                        }
                        // We're basing this assumption on the fact that packages won't change fron version to version
                        // and the package name and version are in the file name
                        _packageCache[path] = package;
                    }

                    packages.Add(package);
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
            // {id}.{version}\{packagefile}.nupack.
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

            base.AddPackage(package);
        }

        public override void RemovePackage(IPackage package) {
            base.RemovePackage(package);

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
    }
}

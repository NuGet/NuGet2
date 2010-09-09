namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class LocalPackageRepository : PackageRepositoryBase {
        private Dictionary<string, Package> _packageCache = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);

        public LocalPackageRepository(string physicalPath)
            : this(new FileBasedProjectSystem(physicalPath)) {
        }

        public LocalPackageRepository(IFileSystem fileSystem) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }

            FileSystem = fileSystem;
        }

        protected IFileSystem FileSystem {
            get;
            private set;
        }

        public override IQueryable<Package> GetPackages() {
            var packages = new List<Package>();
            foreach (var path in GetPackageFiles()) {
                try {
                    Package package;
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
                foreach (var path in FileSystem.GetFiles(dir, "*" + Package.PackageExtension)) {
                    yield return path;
                }
            }

            // Check top level directory
            foreach (var path in FileSystem.GetFiles(String.Empty, "*" + Package.PackageExtension)) {
                yield return path;
            }
        }

        public override void AddPackage(Package package) {
            string packageFilePath = GetPackageFilePath(package);

            FileSystem.AddFile(packageFilePath, stream => PackageBuilder.Save(package, stream));

            base.AddPackage(package);
        }

        public override void RemovePackage(Package package) {
            base.RemovePackage(package);

            // Delete the package file
            string packageFilePath = GetPackageFilePath(package);
            FileSystem.DeleteFile(packageFilePath);

            // Delete the package directory if any
            FileSystem.DeleteDirectory(Utility.GetPackageDirectory(package), true);

            // If this is the last package delete the package directory
            if (!FileSystem.GetFiles(String.Empty).Any() &&
                !FileSystem.GetDirectories(String.Empty).Any()) {
                FileSystem.DeleteDirectory(String.Empty, recursive: false);
            }
        }

        private static string GetPackageFilePath(Package package) {
            return Path.Combine(Utility.GetPackageDirectory(package), Utility.GetPackageFileName(package));
        }
    }
}

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
            foreach (var zipPath in FileSystem.GetFiles(String.Empty, "*" + Utility.PackageExtension)) {
                try {
                    Package package;
                    if (!_packageCache.TryGetValue(zipPath, out package)) {
                        using (Stream stream = FileSystem.OpenFile(zipPath)) {
                            package = new ZipPackage(stream);
                        }
                        // We're basing this assumption on the fact that packages won't change fron version to version
                        // and the package name and version are in the file name
                        _packageCache[zipPath] = package;
                    }

                    packages.Add(package);
                }
                catch (NotSupportedException) {
                    // If this is an unsupported package then skip it
                }
            }
            return packages.AsQueryable();
        }

        public override void AddPackage(Package package) {
            string packageFileName = Utility.GetPackageFileName(package);

            FileSystem.AddFile(packageFileName, package.Save);

            base.AddPackage(package);
        }

        public override void RemovePackage(Package package) {
            base.RemovePackage(package);

            // Delete the package file
            string packageFilePath = Utility.GetPackageFileName(package);
            FileSystem.DeleteFile(packageFilePath);

            // If this is the last package delete the package directory
            if (!FileSystem.GetFiles(String.Empty).Any() &&
                !FileSystem.GetDirectories(String.Empty).Any()) {
                FileSystem.DeleteDirectory(String.Empty, recursive: false);
            }
        }
    }
}

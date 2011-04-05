using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet {
    /// <summary>
    /// The machine cache represents a location on the machine where packages are cached. It is a specific implementation of a local repository and can be used as such.
    /// </summary>
    public class MachineCache : LocalPackageRepository {
        private static readonly MachineCache _default = new MachineCache();

        // Maximum number of packages that can live in this cache.
        private const int MaxPackages = 100;

        // Disable caching since we don't want to cache packages in memory
        private MachineCache()
            : base(GetCachePath(), enableCaching: false) {
        }

        internal MachineCache(IFileSystem fileSystem)
            : base(new DefaultPackagePathResolver(fileSystem), fileSystem, enableCaching: false) {
        }

        public static MachineCache Default {
            get {
                return _default;
            }
        }

        public override void AddPackage(IPackage package) {
            // If we exceed the package count then clear the cache
            var files = GetPackageFiles().ToList();
            if (files.Count >= MaxPackages) {
                Clear(files);
            }

            // We don't want to call RemovePackage here since that does alot more than we need to
            DeletePackage(package);
            base.AddPackage(package);
        }

        private void DeletePackage(IPackage package) {
            string path = GetPackageFilePath(package);
            if (FileSystem.FileExists(path)) {
                // Remove the file if it exists
                FileSystem.DeleteFile(path);
            }
        }

        public void Clear() {
            Clear(GetPackageFiles().ToList());
        }

        private void Clear(IEnumerable<string> files) {
            foreach (var packageFile in files) {
                try {
                    FileSystem.DeleteFile(packageFile);
                }
                catch (FileNotFoundException) {

                }
                catch (UnauthorizedAccessException) {

                }
            }
        }

        protected override string GetPackageFilePath(IPackage package) {
            return Path.GetFileName(base.GetPackageFilePath(package));
        }

        protected override string GetPackageFilePath(string id, Version version) {
            return Path.GetFileName(base.GetPackageFilePath(id, version));
        }

        /// <summary>
        /// The cache path is %LocalAppData%\NuGet\Cache 
        /// </summary>
        private static string GetCachePath() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NuGet", "Cache");
        }
    }
}

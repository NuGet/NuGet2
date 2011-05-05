using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace NuGet {
    /// <summary>
    /// The machine cache represents a location on the machine where packages are cached. It is a specific implementation of a local repository and can be used as such.
    /// </summary>
    public class MachineCache : LocalPackageRepository {
        // Maximum number of packages that can live in this cache.
        private const int MaxPackages = 100; 
        private static readonly Lazy<MachineCache> _instance = new Lazy<MachineCache>(() => CreateDefault(GetCachePath));

        internal MachineCache(IFileSystem fileSystem)
            : base(new DefaultPackagePathResolver(fileSystem), fileSystem, enableCaching: false) {
        }

        public static MachineCache Default {
            get {  return _instance.Value; }
        }

        /// <summary>
        /// Creates a Machine Cache instance, assigns it to the instance variable and returns it.
        /// </summary>
        /// <param name="getCachePath">The method to call to retrieve the path to store files in.</param>
        internal static MachineCache CreateDefault(Func<string> getCachePath) {
            IFileSystem fileSystem;
            try {
                string path = getCachePath();
                fileSystem = new PhysicalFileSystem(path);
            } 
            catch (SecurityException) {
                // We are unable to access the special directory. Create a machine cache using an empty file system
                fileSystem = NullFileSystem.Instance;
            }
            return new MachineCache(fileSystem);
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

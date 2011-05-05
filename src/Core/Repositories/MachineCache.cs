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
        private static readonly object _instanceLock = new object();
        private static MachineCache _instance;

        // Maximum number of packages that can live in this cache.
        private const int MaxPackages = 100;

        // Disable caching since we don't want to cache packages in memory
        private MachineCache(string cachePath)
            : base(cachePath, enableCaching: false) {
        }

        internal MachineCache(IFileSystem fileSystem)
            : base(new DefaultPackagePathResolver(fileSystem), fileSystem, enableCaching: false) {
        }

        public static MachineCache Default {
            get {
                if (_instance == null) {
                    CreateInstance(GetCachePath);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Creates a Machine Cache instance, assigns it to the instance variable and returns it.
        /// </summary>
        /// <param name="getCachePath">The method to call to retrieve the path to store files in.</param>
        internal static MachineCache CreateInstance(Func<string> getCachePath) {
            lock (_instanceLock) {
                if (_instance == null) {
                    try {
                        _instance = new MachineCache(getCachePath());
                    }
                    catch (SecurityException) {
                        // We are unable to access the special directory. Create a machine cache using an empty file system
                        _instance = new MachineCache(new NullFileSystem());
                    }
                }
            }
            return _instance;
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

        private class NullFileSystem : IFileSystem {

            public ILogger Logger {
                get;
                set;
            }

            public string Root {
                get { return String.Empty; }
            }

            public void DeleteDirectory(string path, bool recursive) {
                // Do nothing
            }

            public IEnumerable<string> GetFiles(string path) {
                return Enumerable.Empty<string>();
            }

            public IEnumerable<string> GetFiles(string path, string filter) {
                return Enumerable.Empty<string>();
            }

            public IEnumerable<string> GetDirectories(string path) {
                return Enumerable.Empty<string>();
            }

            public string GetFullPath(string path) {
                return path;
            }

            public void DeleteFile(string path) {
                // Do nothing
            }

            public bool FileExists(string path) {
                return false;
            }

            public bool DirectoryExists(string path) {
                return false;
            }

            public void AddFile(string path, Stream stream) {
                // Do nothing
            }

            public Stream OpenFile(string path) {
                return Stream.Null;
            }

            public DateTimeOffset GetLastModified(string path) {
                return DateTimeOffset.MinValue;
            }

            public DateTimeOffset GetCreated(string path) {
                return DateTimeOffset.MinValue;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace NuGet
{
    /// <summary>
    /// The machine cache represents a location on the machine where packages are cached. It is a specific implementation of a local repository and can be used as such.
    /// </summary>
    public class MachineCache : LocalPackageRepository
    {
        /// <summary>
        /// Maximum number of packages that can live in this cache.
        /// </summary>
        private const int MaxPackages = 100;
        
        private const string NuGetCachePathEnvironmentVariable = "NuGetCachePath";

        private static readonly Lazy<MachineCache> _instance = new Lazy<MachineCache>(() => CreateDefault(GetCachePath));

        internal MachineCache(IFileSystem fileSystem)
            : base(new DefaultPackagePathResolver(fileSystem), fileSystem, enableCaching: false)
        {
        }

        public static MachineCache Default
        {
            get { return _instance.Value; }
        }

        /// <summary>
        /// Creates a Machine Cache instance, assigns it to the instance variable and returns it.
        /// </summary>
        internal static MachineCache CreateDefault(Func<string> getCachePath)
        {
            IFileSystem fileSystem;
            try
            {
                string path = getCachePath();
                if (String.IsNullOrEmpty(path))
                {
                    // If we don't get a path, use a null file system to make the cache object do nothing
                    // This can happen when there is no LocalApplicationData folder
                    fileSystem = NullFileSystem.Instance;
                }
                else
                {
                    fileSystem = new PhysicalFileSystem(path);
                }
            }
            catch (SecurityException)
            {
                // We are unable to access the special directory. Create a machine cache using an empty file system
                fileSystem = NullFileSystem.Instance;
            }
            return new MachineCache(fileSystem);
        }

        public override void AddPackage(IPackage package)
        {
            // If we exceed the package count then clear the cache.
            var files = GetPackageFiles().ToList();
            if (files.Count >= MaxPackages)
            {
                // It's expensive to hit the file system to get the last accessed date for files
                // To reduce this cost from occuring frequently, we'll purge packages in batches allowing for a 20% buffer.
                var filesToDelete = files.OrderBy(FileSystem.GetLastAccessed)
                                         .Take(files.Count - (int)(0.8 * MaxPackages))
                                         .ToList();
                TryClear(filesToDelete);
            }

            string path = GetPackageFilePath(package);
            using (var stream = package.GetStream())
            {
                TryAct(() => FileSystem.AddFile(path, stream));
            }
        }

        public override bool Exists(string packageId, SemanticVersion version)
        {
            string packagePath = GetPackageFilePath(packageId, version);
            return FileSystem.FileExists(packagePath);
        }

        public void Clear()
        {
            TryClear(GetPackageFiles().ToList());
        }

        private void TryClear(IEnumerable<string> files)
        {
            foreach (var packageFile in files)
            {
               TryAct(() => FileSystem.DeleteFileSafe(packageFile));
            }
        }

        protected override string GetPackageFilePath(IPackage package)
        {
            return Path.GetFileName(base.GetPackageFilePath(package));
        }

        protected override string GetPackageFilePath(string id, SemanticVersion version)
        {
            return Path.GetFileName(base.GetPackageFilePath(id, version));
        }

        /// <summary>
        /// Determines the cache path to use for NuGet.exe. By default, NuGet caches files under %LocalAppData%\NuGet\Cache.
        /// This path can be overridden by specifying a value in the NuGetCachePath environment variable.
        /// </summary>
        internal static string GetCachePath() {
            return GetCachePath(Environment.GetEnvironmentVariable, Environment.GetFolderPath);
        }

        internal static string GetCachePath(Func<string, string> getEnvironmentVariable, Func<System.Environment.SpecialFolder, string> getFolderPath)
        {
            string cacheOverride = getEnvironmentVariable(NuGetCachePathEnvironmentVariable);
            if (!String.IsNullOrEmpty(cacheOverride))
            {
                return cacheOverride;
            }
            else
            {
                string localAppDataPath = getFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (String.IsNullOrEmpty(localAppDataPath))
                {
                    return null;
                }
                return Path.Combine(localAppDataPath, "NuGet", "Cache");
            }
        }

        /// <remarks>
        /// We use this method instead of the "safe" methods in FileSystem because it attempts to retry multiple times with delays.
        /// In our case, if we are unable to perform IO over the machine cache, we want to quit trying immediately.
        /// </remarks>
        private static void TryAct(Action action)
        {
            try
            {
                action();
            }
            catch (IOException)
            { 
            }
            catch (UnauthorizedAccessException)
            {
                // Do nothing if this fails. 
            }
        }
    }
}
using System;
using System.IO;
using System.Linq;

namespace NuGet {
    /// <summary>
    /// The machine cache represents a location on the machine where packages are cached. It is a specific implementation of a local repository and can be used as such.
    /// </summary>
    public class MachineCache : PackageRepositoryBase {
        private static readonly MachineCache _default = new MachineCache();

        private readonly string _cacheRoot;

        // Disable caching since we don't want to cache packages in memory
        private MachineCache() {
            _cacheRoot = GetCachePath();
        }

        public static MachineCache Default {
            get {
                return _default;
            }
        }

        public override string Source {
            get {
                return _cacheRoot;
            }
        }

        public override void AddPackage(IPackage package) {
            var existingPackage = FindPackage(package.Id, package.Version);
            if (existingPackage == null) {
                string path = GetPackageFilePath(package);

                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }

                if (!Directory.Exists(path)) {
                    using (Stream stream = File.Create(path)) {
                        package.GetStream().CopyTo(stream);
                    }
                }
            }
        }

        //public string AddPackage(string id, Version version, byte[] bytes) {
        //    string path = GetPackageFilePath(id, version);

        //    if (!Directory.Exists(path)) {
        //        Directory.CreateDirectory(path);
        //    }

        //    if (!Directory.Exists(path)) {
        //        using (Stream stream = File.Create(path)) {
        //            stream.Write(bytes, 0, bytes.Length);
        //        }
        //    }

        //    return path;
        //}

        public override IQueryable<IPackage> GetPackages() {
            throw new NotSupportedException();
        }

        public override void RemovePackage(IPackage package) {
            string path = GetPackageFilePath(package);
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        public IPackage FindPackage(string packageId, Version version) {
            string path = GetPackageFilePath(packageId, version);

            if (File.Exists(path)) {
                return new ZipPackage(path);
            }
            else {
                return null;
            }
        }

        private string GetPackageFilePath(IPackage package) {
            return GetPackageFilePath(package.Id, package.Version);
        }

        private string GetPackageFilePath(string id, Version version) {
            return Path.Combine(Source, id + "." + version.ToString() + Constants.PackageExtension);
        }

        /// <summary>
        /// The cache path is %LocalAppData%\NuGet\Cache 
        /// </summary>
        private static string GetCachePath() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NuGet", "Cache");
        }
    }
}
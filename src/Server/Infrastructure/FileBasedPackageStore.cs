using System;
using System.IO;

namespace NuGet.Server.Infrastructure {
    public class FileBasedPackageStore : IPackageStore {
        string _rootDirectory;

        public FileBasedPackageStore(string packagesDirectory) {
            _rootDirectory = packagesDirectory;
        }

        public string GetFullPath(string path) {
            return Path.Combine(_rootDirectory, path);
        }

        public DateTimeOffset GetLastModified(string path) {
            return File.GetLastWriteTimeUtc(GetFullPath(path));
        }
    }
}

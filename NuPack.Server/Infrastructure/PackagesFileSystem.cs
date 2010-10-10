using System;
using System.Collections.Generic;
using System.IO;

namespace NuPack.Server.Infrastructure {
    public class PackagesFileSystem : IFileSystem {

        public PackagesFileSystem(string packagesDirectory) {
            Root = packagesDirectory;
        }

        public ILogger Logger {
            get;
            set;
        }

        public string Root {
            get;
            private set;
        }

        public string GetFullPath(string path) {
            return Path.Combine(Root, path);
        }

        public void DeleteDirectory(string path, bool recursive) {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetFiles(string path) {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetFiles(string path, string filter) {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetDirectories(string path) {
            throw new NotImplementedException();
        }

        public void DeleteFile(string path) {
            throw new NotImplementedException();
        }

        public bool FileExists(string path) {
            throw new NotImplementedException();
        }

        public bool DirectoryExists(string path) {
            throw new NotImplementedException();
        }

        public void AddFile(string path, System.IO.Stream stream) {
            throw new NotImplementedException();
        }

        public System.IO.Stream OpenFile(string path) {
            throw new NotImplementedException();
        }

        public DateTimeOffset GetLastModified(string path) {
            return File.GetLastWriteTimeUtc(GetFullPath(path));
        }
    }
}
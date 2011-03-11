using System;
using System.Collections.Generic;
using System.IO;

namespace NuGet {
    public class FileSystemWrapper : IExtendedFileSystem {
        public ILogger Logger {
            get {
                throw new NotSupportedException();
            }
            set {
                throw new NotSupportedException();
            }
        }

        public string Root {
            get { throw new NotSupportedException(); }
        }

        public void DeleteDirectory(string path, bool recursive) {
            throw new NotSupportedException();
        }

        public IEnumerable<string> GetFiles(string path) {
            throw new NotSupportedException();
        }

        public IEnumerable<string> GetFiles(string path, string filter) {
            throw new NotSupportedException();
        }

        public IEnumerable<string> GetDirectories(string path) {
            throw new NotSupportedException();
        }

        public void DeleteFile(string path) {
            throw new NotSupportedException();
        }

        public bool FileExists(string path) {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path) {
            return Directory.Exists(path);
        }

        public void AddFile(string path, Stream stream) {
            throw new NotSupportedException();
        }

        public Stream OpenFile(string path) {
            throw new NotSupportedException();
        }

        public DateTime GetLastModified(string path) {
            throw new NotSupportedException();
        }

        public Stream CreateFile(string path) {
            return File.Create(path);
        }

        public string GetFullPath(string path) {
            return Path.GetFullPath(path);
        }


        public string GetCurrentDirectory() {
            return Directory.GetCurrentDirectory();
        }

        DateTimeOffset IFileSystem.GetLastModified(string path) {
            throw new NotSupportedException();
        }

        public DateTimeOffset GetCreated(string path) {
            throw new NotSupportedException();
        }
    }
}

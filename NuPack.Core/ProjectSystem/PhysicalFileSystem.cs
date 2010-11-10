using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Internal.Web.Utils;
using System.IO;
using NuGet.Resources;

namespace NuGet {
    public class PhysicalFileSystem : IFileSystem {
        private readonly string _root;

        public PhysicalFileSystem(string root) {
            if (String.IsNullOrEmpty(root)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "root");
            }
            _root = root;
        }

        public string Root {
            get { return _root; }
        }

        public virtual ILogger Logger { get; set; }

        public virtual string GetFullPath(string path) {
            return Path.Combine(Root, path);
        }

        public virtual void AddFile(string path, Stream stream) {
            EnsureDirectory(Path.GetDirectoryName(path));

            using (Stream outputStream = File.Create(GetFullPath(path))) {
                stream.CopyTo(outputStream);
            }

            WriteAddedFileAndDirectory(path);
        }

        private void WriteAddedFileAndDirectory(string path) {
            string folderPath = Path.GetDirectoryName(path);

            if (!String.IsNullOrEmpty(folderPath)) {
                Logger.Log(MessageLevel.Debug, NuGetResources.Debug_AddedFileToFolder, Path.GetFileName(path), folderPath);
            }
            else {
                Logger.Log(MessageLevel.Debug, NuGetResources.Debug_AddedFile, Path.GetFileName(path));
            }
        }

        public virtual void DeleteFile(string path) {
            if (!FileExists(path)) {
                return;
            }

            try {
                path = GetFullPath(path);
                File.Delete(path);
                string folderPath = Path.GetDirectoryName(path);
                if (!String.IsNullOrEmpty(folderPath)) {
                    Logger.Log(MessageLevel.Debug, NuGetResources.Debug_RemovedFileFromFolder, Path.GetFileName(path), folderPath);
                }
                else {
                    Logger.Log(MessageLevel.Debug, NuGetResources.Debug_RemovedFile, Path.GetFileName(path));
                }
            }
            catch (FileNotFoundException) {

            }
        }

        public virtual void DeleteDirectory(string path) {
            DeleteDirectory(path, recursive: false);
        }

        public virtual void DeleteDirectory(string path, bool recursive) {
            if (!DirectoryExists(path)) {
                return;
            }

            try {
                path = GetFullPath(path);
                Directory.Delete(path, recursive);
                Logger.Log(MessageLevel.Debug, NuGetResources.Debug_RemovedFolder, path);
            }
            catch (DirectoryNotFoundException) {

            }
        }

        public virtual IEnumerable<string> GetFiles(string path) {
            return GetFiles(path, "*.*");
        }

        public virtual IEnumerable<string> GetFiles(string path, string filter) {
            path = EnsureTrailingSlash(GetFullPath(path));
            try {
                if (!Directory.Exists(path)) {
                    return Enumerable.Empty<string>();
                }
                return Directory.EnumerateFiles(path, filter)
                                .Select(MakeRelativePath);
            }
            catch (UnauthorizedAccessException) {

            }
            catch (DirectoryNotFoundException) {

            }

            return Enumerable.Empty<string>();
        }

        public virtual IEnumerable<string> GetDirectories(string path) {
            try {
                path = EnsureTrailingSlash(GetFullPath(path));
                if (!Directory.Exists(path)) {
                    return Enumerable.Empty<string>();
                }
                return Directory.EnumerateDirectories(path)
                                .Select(MakeRelativePath);
            }
            catch (UnauthorizedAccessException) {

            }
            catch (DirectoryNotFoundException) {

            }

            return Enumerable.Empty<string>();
        }

        public virtual DateTimeOffset GetLastModified(string path) {
            if (DirectoryExists(path)) {
                return new DirectoryInfo(GetFullPath(path)).LastWriteTimeUtc;
            }
            return new FileInfo(GetFullPath(path)).LastWriteTimeUtc;
        }

        public virtual bool FileExists(string path) {
            path = GetFullPath(path);
            return File.Exists(path);
        }

        public virtual bool DirectoryExists(string path) {
            path = GetFullPath(path);
            return Directory.Exists(path);
        }

        public virtual Stream OpenFile(string path) {
            path = GetFullPath(path);
            return File.OpenRead(path);
        }

        protected string MakeRelativePath(string fullPath) {
            return fullPath.Substring(Root.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        private void EnsureDirectory(string path) {
            path = GetFullPath(path);
            Directory.CreateDirectory(path);
        }

        private static string EnsureTrailingSlash(string path) {
            if (!path.EndsWith("\\", StringComparison.Ordinal)) {
                path += "\\";
            }
            return path;
        }
    }
}

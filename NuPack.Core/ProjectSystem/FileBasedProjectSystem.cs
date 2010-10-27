namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Internal.Web.Utils;
    using NuGet.Resources;

    public class FileBasedProjectSystem : ProjectSystem {
        private const string BinDir = "bin";
        private string _root;

        public FileBasedProjectSystem(string root) {
            if (String.IsNullOrEmpty(root)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "root");
            }
            _root = root;
        }

        public override string Root {
            get {
                return _root;
            }
        }

        protected string GetFullPath(string path) {
            return Path.Combine(Root, path);
        }

        protected virtual string GetReferencePath(string name) {
            return Path.Combine(BinDir, name);
        }

        public override void AddFile(string path, Stream stream) {
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

        public override void DeleteFile(string path) {
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

        public void DeleteDirectory(string path) {
            DeleteDirectory(path, recursive: false);
        }

        public override void DeleteDirectory(string path, bool recursive) {
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

        public override void AddReference(string referencePath) {
            // Copy to bin by default
            string src = referencePath;
            string referenceName = Path.GetFileName(referencePath);
            string dest = GetFullPath(GetReferencePath(referenceName));

            // Ensure the destination path exists
            Directory.CreateDirectory(Path.GetDirectoryName(dest));

            // Copy the reference over
            File.Copy(src, dest, overwrite: true);
        }

        public override void RemoveReference(string name) {
            DeleteFile(GetReferencePath(name));

            // Delete the bin directory if this was the last reference
            if (!GetFiles(BinDir).Any()) {
                DeleteDirectory(BinDir);
            }
        }

        public override dynamic GetPropertyValue(string propertyName) {
            if(propertyName == null) {
                return null;
            }

            // Return empty string for the root namespace of this project.
            if (propertyName.Equals("RootNamespace", StringComparison.OrdinalIgnoreCase)) {
                return String.Empty;
            }

            return base.GetPropertyValue(propertyName);
        }

        public override IEnumerable<string> GetFiles(string path) {
            return GetFiles(path, "*.*");
        }

        public override IEnumerable<string> GetFiles(string path, string filter) {
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

        public override IEnumerable<string> GetDirectories(string path) {
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

        public override DateTimeOffset GetLastModified(string path) {
            if (DirectoryExists(path)) {
                return new DirectoryInfo(GetFullPath(path)).LastWriteTimeUtc;
            }
            return new FileInfo(GetFullPath(path)).LastWriteTimeUtc;
        }

        public override bool FileExists(string path) {
            path = GetFullPath(path);
            return File.Exists(path);
        }

        public override bool DirectoryExists(string path) {
            path = GetFullPath(path);
            return Directory.Exists(path);
        }

        public override Stream OpenFile(string path) {
            path = GetFullPath(path);
            return File.OpenRead(path);
        }

        public override bool ReferenceExists(string name) {
            string path = GetReferencePath(name);
            return FileExists(path);
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

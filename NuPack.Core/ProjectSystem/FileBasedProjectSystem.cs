namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Internal.Web.Utils;
    using NuPack.Resources;

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
                Listener.OnReportStatus(StatusLevel.Debug, NuPackResources.Debug_AddedFileToFolder, Path.GetFileName(path), folderPath);
            }
            else {
                Listener.OnReportStatus(StatusLevel.Debug, NuPackResources.Debug_AddedFile, Path.GetFileName(path));
            }
        }

        public override void DeleteFile(string path) {
            try {
                path = GetFullPath(path);
                File.Delete(path);
                string folderPath = Path.GetDirectoryName(path);
                if (!String.IsNullOrEmpty(folderPath)) {
                    Listener.OnReportStatus(StatusLevel.Debug, NuPackResources.Debug_RemovedFileFromFolder, Path.GetFileName(path), folderPath);
                }
                else {
                    Listener.OnReportStatus(StatusLevel.Debug, NuPackResources.Debug_RemovedFile, Path.GetFileName(path));
                }
            }
            catch (FileNotFoundException) {

            }
        }

        public override void DeleteDirectory(string path, bool recursive = false) {
            try {
                path = GetFullPath(path);
                Directory.Delete(path, recursive);
                Listener.OnReportStatus(StatusLevel.Debug, NuPackResources.Debug_RemovedFolder, path);
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

        public override IEnumerable<string> GetFiles(string path) {
            return GetFiles(path, "*.*");
        }

        public override IEnumerable<string> GetFiles(string path, string filter) {
            path = EnsureTrailingSlash(GetFullPath(path));
            if (!Directory.Exists(path)) {
                return Enumerable.Empty<string>();
            }
            return Directory.EnumerateFiles(path, filter)
                            .Select(file => file.Substring(path.Length));
        }

        public override IEnumerable<string> GetDirectories(string path) {
            path = EnsureTrailingSlash(GetFullPath(path));
            if (!Directory.Exists(path)) {
                return Enumerable.Empty<string>();
            }
            return Directory.EnumerateDirectories(path)
                            .Select(dir => dir.Substring(path.Length));
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

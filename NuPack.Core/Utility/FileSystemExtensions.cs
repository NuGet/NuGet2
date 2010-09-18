using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuPack.Resources;

namespace NuPack {
    internal static class FileSystemExtensions {
        internal static void AddFiles(this IFileSystem fileSystem,
                                      IEnumerable<IPackageFile> files,
                                      IPackageEventListener listener) {
            AddFiles(fileSystem, files, String.Empty, listener);
        }

        internal static void AddFiles(this IFileSystem fileSystem,
                                      IEnumerable<IPackageFile> files,
                                      string rootDir,
                                      IPackageEventListener listener) {
            foreach (IPackageFile file in files) {
                string path = Path.Combine(rootDir, file.Path);
                AddFile(fileSystem, path, file.Open, listener);
            }
        }
        
        internal static void DeleteFiles(this IFileSystem fileSystem,
                                         IEnumerable<IPackageFile> files,
                                         IPackageEventListener listener) {
            DeleteFiles(fileSystem, files, String.Empty, listener);
        }

        internal static void DeleteFiles(this IFileSystem fileSystem,
                                         IEnumerable<IPackageFile> files,
                                         string rootDir,
                                         IPackageEventListener listener) {

            // First get all directories that contain files
            var directoryLookup = files.ToLookup(p => Path.GetDirectoryName(p.Path));


            // Get all directories that this package may have added
            var directories = from grouping in directoryLookup
                              from directory in GetDirectories(grouping.Key)
                              orderby directory.Length descending
                              select directory;

            // Remove files from every directory
            foreach (var directory in directories) {
                var directoryFiles = directoryLookup.Contains(directory) ? directoryLookup[directory] : Enumerable.Empty<IPackageFile>();
                foreach (var file in directoryFiles) {
                    string path = Path.Combine(rootDir, file.Path);

                    DeleteFile(fileSystem, path, file.Open, listener);
                }

                string dirPath = Path.Combine(rootDir, directory);

                // If the directory is empty then delete it
                if (!fileSystem.GetFiles(dirPath).Any() &&
                    !fileSystem.GetDirectories(dirPath).Any()) {
                    fileSystem.DeleteDirectory(dirPath, recursive: false);
                }
            }
        }

        internal static void DeleteFile(IFileSystem fileSystem, string path, Func<Stream> streamFactory, IPackageEventListener listener) {
            // Only delete the file if it exists and the checksum is the same
            if (fileSystem.FileExists(path)) {
                bool contentEqual;
                using (Stream stream = streamFactory(),
                              fileStream = fileSystem.OpenFile(path)) {
                    contentEqual = stream.ContentEquals(fileStream);
                }

                if (contentEqual) {
                    fileSystem.DeleteFile(path);
                }
                else {
                    // This package installed a file that was modified so warn the user
                    listener.OnReportStatus(StatusLevel.Warning, NuPackResources.Warning_FileModified, path);
                }
            }
        }

        internal static void AddFile(IFileSystem fileSystem, string path, Func<Stream> streamFactory, IPackageEventListener listener) {
            // Don't overwrite file if it exists if force wasn't set to true
            if (fileSystem.FileExists(path)) {
                listener.OnReportStatus(StatusLevel.Warning, NuPackResources.Warning_FileAlreadyExists, path);
            }
            else {
                using (Stream stream = streamFactory()) {
                    fileSystem.AddFile(path, stream);
                }
            }
        }

        internal static IEnumerable<string> GetDirectories(string path) {
            foreach (var index in IndexOfAll(path, Path.DirectorySeparatorChar)) {
                yield return path.Substring(0, index);
            }
            yield return path;
        }

        private static IEnumerable<int> IndexOfAll(string value, char ch) {
            int index = -1;
            do {
                index = value.IndexOf(ch, index + 1);
                if (index >= 0) {
                    yield return index;
                }
            }
            while (index >= 0);
        }

        internal static void AddFile(this IFileSystem fileSystem, string path, Action<Stream> write) {
            using (var stream = new MemoryStream()) {
                write(stream);
                stream.Seek(0, SeekOrigin.Begin);
                fileSystem.AddFile(path, stream);
            }
        }
    }
}
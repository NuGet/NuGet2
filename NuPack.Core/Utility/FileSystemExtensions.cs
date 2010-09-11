namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NuPack.Resources;

    internal static class FileSystemExtensions {
        internal static void AddFiles(this IFileSystem fileSystem,
                                      IEnumerable<IPackageFile> files,
                                      PackageEventListener listener,
                                      Func<IPackageFile, bool> addFileCallback = null,
                                      Func<string, string> pathResolver = null) {
            AddFiles(fileSystem, files, String.Empty, listener, addFileCallback, pathResolver);
        }

        internal static void AddFiles(this IFileSystem fileSystem,
                                      IEnumerable<IPackageFile> files,
                                      string rootDir,
                                      PackageEventListener listener,
                                      Func<IPackageFile, bool> addFileCallback = null,
                                      Func<string, string> pathResolver = null) {
            foreach (IPackageFile file in files) {
                if (addFileCallback != null && addFileCallback(file)) {
                    continue;
                }

                string path = Path.Combine(rootDir, ResolvePath(file.Path, pathResolver));
                // Don't overwrite file if it exists if force wasn't set to true
                if (fileSystem.FileExists(path)) {
                    listener.OnReportStatus(StatusLevel.Warning, NuPackResources.Warning_FileAlreadyExists, path);
                    continue;
                }

                using (Stream packageFileStream = file.Open()) {
                    fileSystem.AddFile(path, packageFileStream);
                }
            }
        }

        internal static void DeleteFiles(this IFileSystem fileSystem,
                                         IEnumerable<IPackageFile> files,
                                         PackageEventListener listener,
                                         Action<IPackageFile> deleteFileCallback = null,
                                         Func<string, string> pathResolver = null) {
            DeleteFiles(fileSystem, files, String.Empty, listener, deleteFileCallback, pathResolver);
        }

        internal static void DeleteFiles(this IFileSystem fileSystem,
                                         IEnumerable<IPackageFile> files,
                                         string rootDir,
                                         PackageEventListener listener,
                                         Action<IPackageFile> deleteFileCallback = null,
                                         Func<string, string> pathResolver = null) {

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
                    if (deleteFileCallback != null) {
                        deleteFileCallback(file);
                    }
                    string path = Path.Combine(rootDir, ResolvePath(file.Path, pathResolver));
                    // Only delete the file if it exists and the checksum is the same
                    if (fileSystem.FileExists(path)) {
                        if (ChecksumEqual(fileSystem, file, path)) {
                            fileSystem.DeleteFile(path);
                        }
                        else {
                            // This package installed a file that was modified so warn the user
                            listener.OnReportStatus(StatusLevel.Warning, NuPackResources.Warning_FileModified, path);
                        }
                    }
                }

                string dirPath = Path.Combine(rootDir, ResolvePath(directory, pathResolver));

                // If the directory is empty then delete it
                if (!fileSystem.GetFiles(dirPath).Any() &&
                    !fileSystem.GetDirectories(dirPath).Any()) {
                    fileSystem.DeleteDirectory(dirPath, recursive: false);
                }
            }
        }

        private static IEnumerable<string> GetDirectories(string path) {
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

        private static bool ChecksumEqual(IFileSystem fileSystem, IPackageFile file, string path) {
            using (Stream projectFileStream = fileSystem.OpenFile(path),
                          packageFileStream = file.Open()) {
                return GetChecksum(projectFileStream) == GetChecksum(packageFileStream);
            }
        }

        private static int GetChecksum(Stream stream) {
            // Copy the stream to a memory steam and get get the CRC32 of the bytes
            using (MemoryStream memoryStream = new MemoryStream()) {
                stream.CopyTo(memoryStream);
                return (int)Crc32.Calculate(memoryStream.ToArray());
            }
        }

        internal static void AddFile(this IFileSystem fileSystem, string path, Action<Stream> write) {
            using (var stream = new MemoryStream()) {
                write(stream);
                stream.Seek(0, SeekOrigin.Begin);
                fileSystem.AddFile(path, stream);
            }
        }

        private static string ResolvePath(string path, Func<string, string> pathSelector) {
            string filePath = path;
            if (pathSelector != null) {
                filePath = pathSelector(path);
            }
            return filePath;
        }
    }
}

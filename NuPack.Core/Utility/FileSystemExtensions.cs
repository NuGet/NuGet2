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
                                      Func<IPackageFile, string> pathSelector = null) {
            AddFiles(fileSystem, files, String.Empty, listener, addFileCallback, pathSelector);
        }

        internal static void AddFiles(this IFileSystem fileSystem, 
                                      IEnumerable<IPackageFile> files, 
                                      string rootDir, 
                                      PackageEventListener listener, 
                                      Func<IPackageFile, bool> addFileCallback = null, 
                                      Func<IPackageFile, string> pathSelector = null) {
            foreach (IPackageFile file in files) {
                if (addFileCallback != null && addFileCallback(file)) {
                    continue;
                }

                string path = Path.Combine(rootDir, GetFilePath(file, pathSelector));
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
                                         Func<IPackageFile, string> pathSelector = null) {
            DeleteFiles(fileSystem, files, String.Empty, listener, deleteFileCallback, pathSelector);
        }

        internal static void DeleteFiles(this IFileSystem fileSystem, 
                                         IEnumerable<IPackageFile> files, 
                                         string rootDir, 
                                         PackageEventListener listener, 
                                         Action<IPackageFile> deleteFileCallback = null, 
                                         Func<IPackageFile, string> pathSelector = null) {
            // Order by longest directory path so we delete the deepest sub folders first
            var fileGroups = from file in files
                             group file by Path.GetDirectoryName(file.Path) into g
                             orderby g.Key.Length descending
                             select g;

            // Remove files
            foreach (var grouping in fileGroups) {
                foreach (var file in grouping) {
                    if (deleteFileCallback != null) {
                        deleteFileCallback(file);
                    }
                    string path = Path.Combine(rootDir, GetFilePath(file, pathSelector));
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

                // If the directory is empty then delete it
                string directory = Path.Combine(rootDir, grouping.Key);
                if (!fileSystem.GetFiles(directory).Any() &&
                    !fileSystem.GetDirectories(directory).Any()) {
                    fileSystem.DeleteDirectory(directory, recursive: false);
                }
            }
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

        private static string GetFilePath(IPackageFile file, Func<IPackageFile, string> pathSelector) {
            string filePath = null;
            if (pathSelector != null) {
                filePath = pathSelector(file);
            }
            if (String.IsNullOrEmpty(filePath)) {
                filePath = file.Path;
            }
            return filePath;
        }
    }
}

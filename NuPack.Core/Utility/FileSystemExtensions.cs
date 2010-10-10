using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuPack.Resources;

namespace NuPack {
    internal static class FileSystemExtensions {
        internal static bool AddFiles(this IFileSystem fileSystem,
                                      IEnumerable<IPackageFile> files,
                                      ILogger logger) {
            return AddFiles(fileSystem, files, String.Empty, logger);
        }

        internal static bool AddFiles(this IFileSystem fileSystem,
                                      IEnumerable<IPackageFile> files,
                                      string rootDir,
                                      ILogger logger) {
            bool success = true;
            foreach (IPackageFile file in files) {
                string path = Path.Combine(rootDir, file.Path);
                if (!AddFile(fileSystem, path, file.Open, logger)) {
                    success = false;
                }
            }
            return success;
        }

        internal static void DeleteFiles(this IFileSystem fileSystem,
                                         IEnumerable<IPackageFile> files,
                                         ILogger logger) {
            DeleteFiles(fileSystem, files, String.Empty, logger);
        }

        internal static bool DeleteFiles(this IFileSystem fileSystem,
                                         IEnumerable<IPackageFile> files,
                                         string rootDir,
                                         ILogger logger) {
            bool success = true;

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
                string dirPath = Path.Combine(rootDir, directory);

                if (!fileSystem.DirectoryExists(dirPath)) {
                    continue;
                }

                foreach (var file in directoryFiles) {
                    string path = Path.Combine(rootDir, file.Path);

                    if (!DeleteFile(fileSystem, path, file.Open, logger)) {
                        success = false;
                    }
                }
                
                // If the directory is empty then delete it
                if (!fileSystem.GetFiles(dirPath).Any() &&
                    !fileSystem.GetDirectories(dirPath).Any()) {
                    if (!DeleteDirectory(fileSystem, dirPath, recursive: false, logger: logger)) {
                        success = false;
                    }
                }
            }
            return success;
        }

        internal static bool DeleteDirectory(IFileSystem fileSystem, string path, bool recursive, ILogger logger) {
            try {
                fileSystem.DeleteDirectory(path, recursive);
            }
            catch (Exception e) {
                logger.Log(MessageLevel.Warning, e.Message);
                return false;
            }
            return true;
        }

        internal static bool DeleteFile(IFileSystem fileSystem, string path) {
            try {
                fileSystem.DeleteFile(path);
            }
            catch (Exception e) {
                fileSystem.Logger.Log(MessageLevel.Warning, e.Message);
                return false;
            }
            return true;
        }

        internal static bool DeleteFile(IFileSystem fileSystem, string path, Func<Stream> streamFactory, ILogger logger) {
            // Only delete the file if it exists and the checksum is the same
            if (fileSystem.FileExists(path)) {
                bool contentEqual;
                using (Stream stream = streamFactory(),
                              fileStream = fileSystem.OpenFile(path)) {
                    contentEqual = stream.ContentEquals(fileStream);
                }

                if (contentEqual) {
                    if (!DeleteFile(fileSystem, path)) {
                        return false;
                    }
                }
                else {
                    // This package installed a file that was modified so warn the user
                    logger.Log(MessageLevel.Warning, NuPackResources.Warning_FileModified, path);
                }
            }
            return true;
        }

        internal static bool AddFile(IFileSystem fileSystem, string path, Func<Stream> streamFactory, ILogger logger) {
            // Don't overwrite file if it exists if force wasn't set to true
            if (fileSystem.FileExists(path)) {
                logger.Log(MessageLevel.Warning, NuPackResources.Warning_FileAlreadyExists, path);
            }
            else {
                using (Stream stream = streamFactory()) {
                    try {
                        fileSystem.AddFile(path, stream);
                    }
                    catch (Exception e) {
                        logger.Log(MessageLevel.Warning, e.Message);
                        return false;
                    }
                }
            }
            return true;
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
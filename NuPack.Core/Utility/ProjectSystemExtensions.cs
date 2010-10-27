using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace NuGet {
    // REVIEW: Do we need this class? Should this logic be moved to ProjectManager?
    public static class ProjectSystemExtensions {
        public static void AddFiles(this ProjectSystem project, 
                                    IEnumerable<IPackageFile> files, 
                                    IDictionary<string, IPackageFileTransformer> fileTransformers) {
            foreach (IPackageFile file in files) {
                // Remove the redundant folder from the path
                string path = RemoveContentDirectory(file.Path);

                // Try to get the package file modifier for the extension
                string extension = Path.GetExtension(file.Path);
                IPackageFileTransformer transformer;
                if (fileTransformers.TryGetValue(extension, out transformer)) {
                    // Remove the extension to get the target path
                    path = RemoveExtension(path);

                    // If the transform was done then continue
                    transformer.TransformFile(file, path, project);
                }
                else {
                    project.AddFileWithCheck(path, file.GetStream);
                }
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static void DeleteFiles(this ProjectSystem project, 
                                       IEnumerable<IPackageFile> files, 
                                       IEnumerable<IPackage> otherPackages, 
                                       IDictionary<string, IPackageFileTransformer> fileTransformers) {

            // First get all directories that contain files
            var directoryLookup = files.ToLookup(p => Path.GetDirectoryName(p.Path));


            // Get all directories that this package may have added
            var directories = from grouping in directoryLookup
                              from directory in FileSystemExtensions.GetDirectories(grouping.Key)
                              orderby directory.Length descending
                              select directory;

            // Remove files from every directory
            foreach (var directory in directories) {
                var directoryFiles = directoryLookup.Contains(directory) ? directoryLookup[directory] : Enumerable.Empty<IPackageFile>();

                string dirPath = RemoveContentDirectory(directory);
                if (!project.DirectoryExists(dirPath)) {
                    continue;
                }

                foreach (var file in directoryFiles) {
                    // Remove the content folder from the path
                    string path = RemoveContentDirectory(file.Path);
                    // Try to get the package file modifier for the extension
                    string extension = Path.GetExtension(file.Path);
                    IPackageFileTransformer transformer;
                    if (fileTransformers.TryGetValue(extension, out transformer)) {
                        // Remove the extension to get the target path
                        path = RemoveExtension(path);

                        var matchingFiles = from p in otherPackages
                                            from otherFile in p.GetContentFiles()
                                            where otherFile.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase)
                                            select otherFile;

                        transformer.RevertFile(file, path, matchingFiles, project);
                    }
                    else {
                        project.DeleteFileSafe(path, file.GetStream);
                    }
                }

                // If the directory is empty then delete it
                if (!project.GetFilesSafe(dirPath).Any() &&
                    !project.GetDirectoriesSafe(dirPath).Any()) {
                        project.DeleteDirectorySafe(dirPath, recursive: false);
                }
            }
        }

        private static string RemoveContentDirectory(string path) {
            Debug.Assert(path.StartsWith(Constants.ContentDirectory, StringComparison.OrdinalIgnoreCase));

            return path.Substring(Constants.ContentDirectory.Length).TrimStart('\\');
        }

        private static string RemoveExtension(string path) {
            // Remove the extension from the file name, preserving the directory
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        }
    }
}

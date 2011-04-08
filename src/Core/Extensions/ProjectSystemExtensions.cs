using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using NuGet.Resources;

namespace NuGet {
    // REVIEW: Do we need this class? Should this logic be moved to ProjectManager?
    public static class ProjectSystemExtensions {
        public static void AddFiles(this IProjectSystem project,
                                    IEnumerable<IPackageFile> files,
                                    IDictionary<string, IPackageFileTransformer> fileTransformers) {
            // BUG 636: We add the files with the longest path first so that vs picks up code behind files.
            // This shouldn't matter for any other scenario.
            foreach (IPackageFile file in files.OrderByDescending(p => p.Path)) {
                // Remove the redundant folder from the path
                string path = ResolvePath(project, file.Path);

                // Try to get the package file modifier for the extension
                string extension = Path.GetExtension(file.Path);
                IPackageFileTransformer transformer;
                if (fileTransformers.TryGetValue(extension, out transformer)) {
                    // Remove the extension to get the target path
                    path = RemoveExtension(path);

                    if (project.IsSupportedFile(path)) {
                        // If the transform was done then continue
                        transformer.TransformFile(file, path, project);
                    }
                }
                else if (project.IsSupportedFile(path)) {
                    project.AddFileWithCheck(path, file.GetStream);
                }
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want delete to be robust, when exceptions occur we log then and move on")]
        public static void DeleteFiles(this IProjectSystem project,
                                       IEnumerable<IPackageFile> files,
                                       IEnumerable<IPackage> otherPackages,
                                       IDictionary<string, IPackageFileTransformer> fileTransformers) {

            // First get all directories that contain files
            var directoryLookup = files.ToLookup(p => Path.GetDirectoryName(ResolvePath(project, p.Path)));


            // Get all directories that this package may have added
            var directories = from grouping in directoryLookup
                              from directory in FileSystemExtensions.GetDirectories(grouping.Key)
                              orderby directory.Length descending
                              select directory;

            // Remove files from every directory
            foreach (var directory in directories) {
                var directoryFiles = directoryLookup.Contains(directory) ? directoryLookup[directory] : Enumerable.Empty<IPackageFile>();

                if (!project.DirectoryExists(directory)) {
                    continue;
                }

                foreach (var file in directoryFiles) {
                    // Remove the content folder from the path
                    string path = ResolvePath(project, file.Path);

                    // Try to get the package file modifier for the extension
                    string extension = Path.GetExtension(file.Path);
                    IPackageFileTransformer transformer;
                    if (fileTransformers.TryGetValue(extension, out transformer)) {
                        // Remove the extension to get the target path
                        path = RemoveExtension(path);

                        if (project.IsSupportedFile(path)) {

                            var matchingFiles = from p in otherPackages
                                                from otherFile in p.GetContentFiles()
                                                where otherFile.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase)
                                                select otherFile;

                            try {
                                transformer.RevertFile(file, path, matchingFiles, project);
                            }
                            catch (Exception e) {
                                // Report a warning and move on
                                project.Logger.Log(MessageLevel.Warning, e.Message);
                            }
                        }
                    }
                    else if (project.IsSupportedFile(path)) {
                        project.DeleteFileSafe(path, file.GetStream);
                    }
                }

                // If the directory is empty then delete it
                if (!project.GetFilesSafe(directory).Any() &&
                    !project.GetDirectoriesSafe(directory).Any()) {
                    project.DeleteDirectorySafe(directory, recursive: false);
                }
            }
        }

        public static IEnumerable<T> GetCompatibleItems<T>(this IProjectSystem project, IEnumerable<T> items, string itemType) where T : IFrameworkTargetable {
            // A package might have references that target a specific version of the framework (.net/silverlight etc)
            // so we try to get the highest version that satifies the target framework i.e.
            // if a package has 1.0, 2.0, 4.0 and the target framework is 3.5 we'd pick the 2.0 references.
            IEnumerable<T> compatibleItems;
            if (!project.TryGetCompatibleItems(items, out compatibleItems)) {
                throw new InvalidOperationException(
                           String.Format(CultureInfo.CurrentCulture,
                           NuGetResources.UnableToFindCompatibleItems, itemType, project.TargetFramework));
            }

            return compatibleItems;
        }

        public static bool TryGetCompatibleItems<T>(this IProjectSystem projectSystem, IEnumerable<T> items, out IEnumerable<T> compatibleItems) where T : IFrameworkTargetable {
            if (projectSystem == null) {
                throw new ArgumentNullException("projectSystem");
            }

            if (items == null) {
                throw new ArgumentNullException("items");
            }

            return VersionUtility.TryGetCompatibleItems<T>(projectSystem.TargetFramework, items, out compatibleItems);
        }

        internal static IEnumerable<T> GetCompatibleItemsCore<T>(this IProjectSystem projectSystem, IEnumerable<T> items) where T : IFrameworkTargetable {
            IEnumerable<T> compatibleItems;
            if (VersionUtility.TryGetCompatibleItems(projectSystem.TargetFramework, items, out compatibleItems)) {
                return compatibleItems;
            }
            return Enumerable.Empty<T>();
        }

        private static string ResolvePath(IProjectSystem projectSystem, string path) {
            return projectSystem.ResolvePath(RemoveContentDirectory(path));
        }

        private static string RemoveContentDirectory(string path) {
            Debug.Assert(path.StartsWith(Constants.ContentDirectory, StringComparison.OrdinalIgnoreCase));

            return path.Substring(Constants.ContentDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        private static string RemoveExtension(string path) {
            // Remove the extension from the file name, preserving the directory
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        }
    }
}

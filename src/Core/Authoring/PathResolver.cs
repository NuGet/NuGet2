using System;
using System.Diagnostics;
using System.IO;

namespace NuGet {
    internal static class PathResolver {
        public static PathSearchFilter ResolvePath(string basePath, string source) {
            basePath = basePath ?? String.Empty;
            string pathFromBase = Path.Combine(basePath, source.TrimStart(Path.DirectorySeparatorChar));

            if (!IsAbsolutePathFilter(pathFromBase)) {
                return GetPathSearchFilter(pathFromBase);
            }
            else {
                pathFromBase = Path.GetFullPath(pathFromBase.TrimStart(Path.DirectorySeparatorChar));
                string directory = Path.GetDirectoryName(pathFromBase);
                string searchFilter = Path.GetFileName(pathFromBase);
                return new PathSearchFilter {
                    SearchDirectory = NormalizeSearchDirectory(directory),
                    SearchPattern = NormalizeSearchFilter(searchFilter),
                    SearchOption = SearchOption.TopDirectoryOnly,
                    IsAbsolutePathFilter = true
                };
            }
        }

        private static PathSearchFilter GetPathSearchFilter(string path) {
            Debug.Assert(!IsAbsolutePathFilter(path));

            var searchFilter = new PathSearchFilter();
            int recursiveSearchIndex = path.IndexOf("**", StringComparison.OrdinalIgnoreCase);

            if (recursiveSearchIndex != -1) {
                // Recursive searches are of the format /foo/bar/**/*[.abc]
                string searchPattern = path.Substring(recursiveSearchIndex + 2).TrimStart(Path.DirectorySeparatorChar);
                string searchDirectory = recursiveSearchIndex == 0 ? "." : path.Substring(0, recursiveSearchIndex - 1);
                
                searchFilter.SearchDirectory = NormalizeSearchDirectory(searchDirectory);
                searchFilter.SearchPattern = NormalizeSearchFilter(searchPattern);
                searchFilter.SearchOption = SearchOption.AllDirectories;
            }
            else {
                string searchDirectory = Path.GetDirectoryName(path);
                string searchPattern = Path.GetFileName(path);

                searchFilter.SearchDirectory = NormalizeSearchDirectory(searchDirectory);
                searchFilter.SearchPattern = NormalizeSearchFilter(searchPattern);
                searchFilter.SearchOption = SearchOption.TopDirectoryOnly;
            }

            return searchFilter;
        }

        /// <summary>
        /// Resolves the path of a file inside of a package 
        /// For paths that are relative, the destination path is resovled as the path relative to the basePath (path to the manifest file)
        /// For all other paths, the path is resolved as the first path portion that does not contain a wildcard character
        /// </summary>
        public static string ResolvePackagePath(string searchString, string basePath, string actualPath, string targetPath) {
            Debug.Assert(searchString != null);
            if (String.IsNullOrEmpty(basePath)) {
                basePath = ".";
            }
            basePath = Path.GetFullPath(basePath);
            actualPath = Path.GetFullPath(actualPath);
            string actualFileName = Path.GetFileName(actualPath);

            string packagePath = null;
            int searchWildCard = searchString.IndexOf("*", StringComparison.OrdinalIgnoreCase);

            if (IsAbsolutePathFilter(searchString) && actualFileName.Equals(Path.GetFileName(searchString), StringComparison.OrdinalIgnoreCase)) {
                // If the search path looks like an absolute path to a file, then 
                // (a) If the target path shares the same extension, copy it
                // e.g. <file src="ie\css\style.css" target="Content\css\ie.css" /> --> Content\css\ie.css
                // (b) Else the file would be at the root of the target.
                // e.g. <file src="foo\bar\baz.dll" target="lib" /> --> lib\baz.dll
                if (Path.GetExtension(searchString).Equals(Path.GetExtension(targetPath), StringComparison.OrdinalIgnoreCase)) {
                    return targetPath;
                }
                else {
                    packagePath = actualFileName;
                }
            }
            else if (searchWildCard > 0) {
                // If the search path in the manifest does not contain a wildcard character 
                // or the wildcard is at the start of search path, do not truncate the search path from the actualPath
                searchString = searchString.Substring(0, searchWildCard - 1);

                // Ignore any occurences of the searchString in the basePath portion of the actualPath.
                // e.g. actualPath: C:\test\release\foo\test\release\bar.txt, basePath: C:\test\release, searchPath: test\release\*.txt. 
                // In this case ignore the first "test\release" that occurs in the basePath.
                int offset = actualPath.IndexOf(basePath, StringComparison.OrdinalIgnoreCase);
                offset = offset == -1 ? 0 : offset + basePath.Length;

                int index = actualPath.IndexOf(searchString, offset > 0 ? offset : 0, StringComparison.OrdinalIgnoreCase);
                packagePath = actualPath.Substring(index + searchString.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            else if (actualPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) {
                packagePath = actualPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            else {
                packagePath = Path.GetFileName(actualPath);
            }

            return Path.Combine(targetPath ?? String.Empty, packagePath);
        }

        private static string NormalizeSearchDirectory(string directory) {
            return Path.GetFullPath(String.IsNullOrEmpty(directory) ? "." : directory);
        }

        private static string NormalizeSearchFilter(string filter) {
            return String.IsNullOrEmpty(filter) ? "*" : filter;
        }

        /// <summary>
        /// Returns true if the path does not contain any wildcards.
        /// </summary>
        private static bool IsAbsolutePathFilter(string filter) {
            return filter.IndexOf('*') == -1;
        }
    }
}

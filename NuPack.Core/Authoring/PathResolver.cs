using System;
using System.Diagnostics;
using System.IO;

namespace NuGet {
    internal static class PathResolver {
        public static PathSearchFilter ResolvePath(string basePath, string source) {
            basePath = basePath ?? String.Empty;
            string pathFromBase = Path.Combine(basePath, source.TrimStart(Path.DirectorySeparatorChar));

            if (pathFromBase.Contains("*")) {
                return GetPathSearchFilter(pathFromBase);
            }
            else {
                pathFromBase = Path.GetFullPath(pathFromBase.TrimStart(Path.DirectorySeparatorChar));
                string directory = Path.GetDirectoryName(pathFromBase);
                string searchFilter = Path.GetFileName(pathFromBase);
                return new PathSearchFilter(NormalizeSearchDirectory(directory), NormalizeSearchFilter(searchFilter), SearchOption.TopDirectoryOnly);
            }
        }

        private static PathSearchFilter GetPathSearchFilter(string path) {
            int recursiveSearchIndex = path.IndexOf("**", StringComparison.OrdinalIgnoreCase);
            if (recursiveSearchIndex != -1) {
                // Recursive searches are of the format /foo/bar/**/*[.abc]
                string searchPattern = path.Substring(recursiveSearchIndex + 2).TrimStart(Path.DirectorySeparatorChar);
                string searchDirectory = recursiveSearchIndex == 0 ? "." : path.Substring(0, recursiveSearchIndex - 1);
                return new PathSearchFilter(NormalizeSearchDirectory(searchDirectory), NormalizeSearchFilter(searchPattern), SearchOption.AllDirectories);
            }
            else {
                string searchDirectory;
                searchDirectory = Path.GetDirectoryName(path);
                if (String.IsNullOrEmpty(searchDirectory)) {
                    // Path starts with a wildcard e.g. *, *.foo, *foo, foo*
                    // Set the current directory to be the search path
                    searchDirectory = ".";
                }
                string searchPattern = Path.GetFileName(path);

                return new PathSearchFilter(NormalizeSearchDirectory(searchDirectory), NormalizeSearchFilter(searchPattern),
                    SearchOption.TopDirectoryOnly);
            }
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

            if ((searchWildCard == -1) && actualFileName.Equals(Path.GetFileName(searchString), StringComparison.OrdinalIgnoreCase)) {
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
                // e.g. actualPath: C:\foo\foo\foo.txt, basePath: C:\foo, searchPath: foo\*.txt. In this case ignore the first foo.
                int offset = actualPath.IndexOf(basePath, StringComparison.OrdinalIgnoreCase);

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
    }
}

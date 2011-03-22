using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NuGet {
    public static class PathResolver {
        /// <summary>
        /// Returns a collection of files from the source that matches the wildcard.
        /// </summary>
        /// <param name="source">The collection of files to match.</param>
        /// <param name="getPath">Function that returns the path to filter a package file </param>
        /// <param name="wildCard">The wildcard to apply to match the path with.</param>
        /// <returns></returns>
        public static IEnumerable<T> GetMatches<T>(IEnumerable<T> source, Func<T, string> getPath, IEnumerable<string> wildcards) where T : IPackageFile {
            var filters = wildcards.Select(WildCardToRegex);
            return source.Where(item => {
                string path = getPath(item);
                return filters.Any(f => f.IsMatch(path));
            });
        }

        public static void FilterPackageFiles<T>(ICollection<T> source, Func<T, string> getPath, IEnumerable<string> wildcards) where T : IPackageFile {
            var matchedFiles = new HashSet<T>(GetMatches(source, getPath, wildcards));
            source.RemoveAll(p => matchedFiles.Contains(p));
        }

        private static Regex WildCardToRegex(string wildCard) {
            return new Regex('^'
               + Regex.Escape(wildCard)
                .Replace(@"\*\*\\", ".*") //For recursive wildcards \**\, include the current directory.
                .Replace(@"\*\*", ".*") // For recursive wildcards that don't end in a slash e.g. **.txt would be treated as a .txt file at any depth
                .Replace(@"\*", @"[^\\]*(\\)?") // For non recursive searches, limit it any character that is not a directory separator
                .Replace(@"\?", ".") // ? translates to a single any character
               + '$', RegexOptions.IgnoreCase);
        }

        internal static PathSearchFilter ResolveSearchFilter(string basePath, string source) {
            basePath = basePath ?? String.Empty;
            string pathFromBase = Path.Combine(basePath, source.TrimStart(Path.DirectorySeparatorChar));

            if (IsWildCardSearch(pathFromBase)) {
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
                    WildCardSearch = false
                };
            }
        }

        private static PathSearchFilter GetPathSearchFilter(string path) {
            Debug.Assert(IsWildCardSearch(path));

            var searchFilter = new PathSearchFilter { WildCardSearch = true };
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
        /// </summary>
        /// <param name="searchFilter">The search filter used to add the file.</param>
        /// <param name="path">The absolute path to the file being added.</param>
        /// <param name="targetPath">The target path prefix for the .</param>
        internal static string ResolvePackagePath(PathSearchFilter searchFilter, string path, string targetPath) {
            string packagePath = null;
            if (!searchFilter.WildCardSearch && Path.GetExtension(searchFilter.SearchPattern).Equals(Path.GetExtension(targetPath), StringComparison.OrdinalIgnoreCase)) {
                // If the search does not contain wild cards, and the target path shares the same extension, copy it
                // e.g. <file src="ie\css\style.css" target="Content\css\ie.css" /> --> Content\css\ie.css
                return targetPath;
            }
            else if (path.StartsWith(searchFilter.SearchDirectory, StringComparison.OrdinalIgnoreCase)) {
                // The SearchDirectory property contains the path leading up to the first wildcard or the complete directory path for includes without wildcard. 
                // e.g. Search: X:\foo\**\*.cs results in SearchDirectory: X:\foo and a file path of X:\foo\bar\biz\boz.cs
                // e.g. Search: X:\foo\bar.cs results in SearchDirectory: X:\foo and a file path X:\foo\bar.cs
                // Truncating X:\foo\ would result in the package path.
                packagePath = path.Substring(searchFilter.SearchDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            else {
                // Review: When would we ever come to this condition?
                packagePath = Path.GetFileName(path);
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
        private static bool IsWildCardSearch(string filter) {
            return filter.IndexOf('*') != -1;
        }
    }
}
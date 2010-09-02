using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NuPack {
    internal static class PathResolver {
        public static IEnumerable<AuthoringPackageFile> ResolvePath(string basePath, string source, string destination) {
            basePath = basePath ?? String.Empty;
            destination = destination ?? String.Empty;
            string pathFromBase = Path.Combine(basePath, source);
            return from path in ResolveSourcePath(pathFromBase)
                   let destinationPath = ResolveDestinationPath(destination, pathFromBase, path)
                   select new AuthoringPackageFile { Path = destinationPath, 
                       Name = Path.GetFileName(pathFromBase), 
                       SourceStream = () => File.OpenRead(pathFromBase) } ;
        }

        private static string ResolveDestinationPath(string destinationBase, string sourcePath, string actualPath) {
            string fileName = Path.GetFileName(actualPath);
            string sourcePathDir = Path.GetDirectoryName(sourcePath).TrimEnd('*');
            string actualPathDir = Path.GetDirectoryName(actualPath);
            string packageRelativePath = String.Empty;
            //If there's a directory structure in the actual path, we need to recreate it
            int index = actualPathDir.IndexOf(sourcePathDir);
            if (index != -1 && (index + sourcePathDir.Length) < actualPathDir.Length) {
                packageRelativePath = actualPathDir.Substring(index + sourcePathDir.Length);
            }
            return Path.Combine(destinationBase, packageRelativePath, fileName);
        }

        private static IEnumerable<string> ResolveSourcePath(string filePath) {
            if (IsFullPath(filePath)) {
                //The path contains a single file. Since no directory structure can be constructed, return
                return new[] { filePath };
            }
            else {
                PathSearchFilter searchFilter = GetPathSearchFilter(filePath);
                return Directory.EnumerateFiles(searchFilter.SearchDirectory, searchFilter.SearchPattern, searchFilter.SearchOption);
            }
        }

        private static bool IsFullPath(string filePath) {
            // Any path that contains a file name and does not contain a wild card is considered a full path
            return !filePath.Contains('*') && !String.IsNullOrEmpty(Path.GetFileName(filePath));
        }

        private static PathSearchFilter GetPathSearchFilter(string path) {
            var searchFilter = new PathSearchFilter { SearchDirectory = path, SearchOption = SearchOption.TopDirectoryOnly };
            bool recursiveWildCardSearch = path.Contains("**"), wildCardSearch = path.Contains('*');
            string fileName = Path.GetFileName(path);

            searchFilter.SearchOption = recursiveWildCardSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            searchFilter.SearchDirectory = Path.GetDirectoryName(path).TrimEnd('*', Path.DirectorySeparatorChar);

            if (String.IsNullOrEmpty(searchFilter.SearchDirectory)) {
                searchFilter.SearchDirectory = ".";
            }

            searchFilter.SearchPattern = String.IsNullOrEmpty(fileName) ? "*" : fileName;
            return searchFilter;
        }

        private class PathSearchFilter {
            public string SearchDirectory { get; set; }

            public SearchOption SearchOption { get; set; }

            public string SearchPattern { get; set; }
        }
    }
}

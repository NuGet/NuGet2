using System;
using System.IO;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace NuGet {
    public static class PathUtility {
        public static string EnsureTrailingSlash(string path) {
            if (path == null) {
                throw new ArgumentNullException("path");
            }
            int length = path.Length;
            if ((length != 0) && (path[length - 1] != Path.DirectorySeparatorChar)) {
                return path + Path.DirectorySeparatorChar;
            }
            return path;
        }

        /// <summary>
        /// Returns path2 relative to path1
        /// </summary>
        public static string GetRelativePath(string path1, string path2) {
            if (path1 == null) {
                throw new ArgumentNullException("path1");
            }

            if (path2 == null) {
                throw new ArgumentNullException("path2");
            }

            Uri source = new Uri(path1);
            Uri target = new Uri(path2);

            return UriUtility.GetPath(source.MakeRelativeUri(target));
        }

        public static string GetAbsolutePath(string basePath, string relativePath) {
            if (basePath == null) {
                throw new ArgumentNullException("basePath");
            }

            if (relativePath == null) {
                throw new ArgumentNullException("relativePath");
            }

            Uri resultUri = new Uri(new Uri(basePath), new Uri(relativePath, UriKind.Relative));
            return resultUri.LocalPath;
        }
    }
}

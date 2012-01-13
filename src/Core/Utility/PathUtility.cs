using System;
using System.IO;

namespace NuGet
{
    public static class PathUtility
    {
        public static bool IsSubdirectory(string basePath, string path)
        {
            if (basePath == null)
            {
                throw new ArgumentNullException("basePath");
            }
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            basePath = basePath.TrimEnd(Path.DirectorySeparatorChar);
            return path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
        }

        public static string EnsureTrailingSlash(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            int length = path.Length;
            if ((length != 0) && (path[length - 1] != Path.DirectorySeparatorChar))
            {
                return path + Path.DirectorySeparatorChar;
            }
            return path;
        }

        /// <summary>
        /// Returns path2 relative to path1
        /// </summary>
        public static string GetRelativePath(string path1, string path2)
        {
            if (path1 == null)
            {
                throw new ArgumentNullException("path1");
            }

            if (path2 == null)
            {
                throw new ArgumentNullException("path2");
            }

            Uri source = new Uri(path1);
            Uri target = new Uri(path2);

            return UriUtility.GetPath(source.MakeRelativeUri(target));
        }

        public static string GetAbsolutePath(string basePath, string relativePath)
        {
            if (basePath == null)
            {
                throw new ArgumentNullException("basePath");
            }

            if (relativePath == null)
            {
                throw new ArgumentNullException("relativePath");
            }

            Uri resultUri = new Uri(new Uri(basePath), new Uri(relativePath, UriKind.Relative));
            return resultUri.LocalPath;
        }

        public static string GetCanonicalPath(string path)
        {
            if (PathValidator.IsValidLocalPath(path) || (PathValidator.IsValidUncPath(path)))
            {
                return Path.GetFullPath(EnsureTrailingSlash(path));
            }
            if (PathValidator.IsValidUrl(path))
            {
                var url = new Uri(path);
                // return canonical representation of Uri
                return url.ToString();
            }
            return path;
        }

        //public static bool PathEquals(string pathA, string pathB)
        //{
        //    if (pathA == null)
        //    {
        //        throw new ArgumentNullException("pathA");
        //    }

        //    if (pathB == null)
        //    {
        //        throw new ArgumentNullException("pathB");
        //    }

        //    return pathA.TrimEnd(Path.DirectorySeparatorChar).Equals(pathB.TrimEnd(Path.DirectorySeparatorChar));
        //}
    }
}
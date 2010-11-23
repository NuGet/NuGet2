using System;
using System.IO;
using System.Text;

namespace NuGet {
    internal class PathUtility {
        /// <summary>
        /// Returns path2 relative to path1
        /// </summary>
        public static string GetRelativePath(string path1, string path2) {
            return GetRelativePath(path1, path2, Directory.Exists);
        }

        public static string GetRelativePath(string path1, string path2, Func<string, bool> isDirectory) {
            if (path1 == null) {
                throw new ArgumentNullException("path1");
            }

            if (path2 == null) {
                throw new ArgumentNullException("path2");
            }

            string[] path1Segments = path1.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            string[] path2Segments = path2.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            int index = 0;
            while (index < Math.Min(path1Segments.Length, path2Segments.Length)) {
                if (!String.Equals(path1Segments[index], path2Segments[index], StringComparison.OrdinalIgnoreCase)) {
                    break;
                }
                index++;
            }

            if (index == 0) {
                return path2;
            }

            var builder = new StringBuilder();
            for (int i = index; i < path1Segments.Length; i++) {
                // REVIEW: Perf?
                // Get the full path
                string fullPath = String.Join(@"\", path1Segments, 0, index + 1);

                // If it's a directory then append ..\
                if (isDirectory(fullPath)) {
                    builder.Append(@"..\");
                }
            }

            for (int i = index; i < path2Segments.Length; i++) {
                builder.Append(path2Segments[i]);
                builder.Append(Path.DirectorySeparatorChar);
            }

            return builder.ToString().TrimEnd(Path.DirectorySeparatorChar);
        }
    }
}

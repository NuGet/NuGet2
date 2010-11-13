using System;
using System.IO;
using System.Text;

namespace NuGet {
    internal class PathUtility {
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

            if (path1.StartsWith(@"\\", StringComparison.Ordinal)) {
                path1 = path1.Substring(2);
            }

            if (path2.StartsWith(@"\\", StringComparison.Ordinal)) {
                path2 = path2.Substring(2);
            }

            string[] path1Segments = path1.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string[] path2Segments = path2.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

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
                if (!Path.HasExtension(path1Segments[i])) {
                    builder.Append(@"..\");
                }
            }

            for (int i = index; i < path2Segments.Length; i++) {
                builder.Append(path2Segments[i]);
                builder.Append('\\');
            }

            return builder.ToString().Trim(new char[] { '\\' });

        }
    }
}

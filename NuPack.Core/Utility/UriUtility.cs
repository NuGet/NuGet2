using System;
using System.IO.Packaging;
using System.IO;

namespace NuGet {    
    internal static class UriUtility {
        internal static string GetPath(Uri uri) {
            string path = uri.OriginalString;
            if (path.StartsWith("/", StringComparison.Ordinal)) {
                path = path.Substring(1);
            }
            // Change the direction of the slashes to match the filesystem.
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        internal static Uri CreatePartUri(string path) {
            return PackUriHelper.CreatePartUri(new Uri(path, UriKind.Relative));
        }
    }
}

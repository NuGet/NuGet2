namespace NuGet {
    using System;
    using System.IO.Packaging;

    internal static class UriHelper {
        internal static string GetPath(Uri uri) {
            string path = uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.Unescaped);
            if (path.StartsWith("/", StringComparison.Ordinal)) {
                path = path.Substring(1);
            }
            // Change the direction of the slashes
            return path.Replace('/', '\\');
        }

        internal static Uri CreatePartUri(string path) {
            return PackUriHelper.CreatePartUri(new Uri(path, UriKind.Relative));
        }
    }
}

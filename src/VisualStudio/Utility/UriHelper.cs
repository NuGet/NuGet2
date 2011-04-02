using System;

namespace NuGet.VisualStudio {
    public static class UriHelper {

        public static void OpenExternalLink(Uri url) {
            if (url == null) {
                return;
            }

            // mitigate security risk
            if (url.IsFile || url.IsLoopback || url.IsUnc) {
                return;
            }

            if (IsHttpUrl(url)) {
                // REVIEW: Will this allow a package author to execute arbitrary program on user's machine?
                // We have limited the url to be HTTP only, but is it sufficient?
                System.Diagnostics.Process.Start(url.AbsoluteUri);
            }
        }

        public static bool IsHttpSource(string source) {
            if (String.IsNullOrEmpty(source)) {
                return false;
            }

            Uri uri;
            if (Uri.TryCreate(source, UriKind.Absolute, out uri)) {
                return IsHttpUrl(uri);
            }
            else {
                return false;
            }
        }

        private static bool IsHttpUrl(Uri uri) {
            return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
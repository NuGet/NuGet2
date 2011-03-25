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

            string scheme = url.Scheme;
            if (scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) {
                // REVIEW: Will this allow a package author to execute arbitrary program on user's machine?
                // We have limited the url to be HTTP only, but is it sufficient?
                System.Diagnostics.Process.Start(url.AbsoluteUri);
            }
        }
    }
}

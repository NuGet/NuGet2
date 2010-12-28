using System;
using System.Web;
using System.Globalization;

namespace NuGet.Server {
    public static class HttpContextExtensions {
        public static bool IsUnmodified(this HttpRequestBase request, DateTimeOffset resourceLastModified) {
            DateTimeOffset ifModifiedSince;

            // If-Modified-Since in format "Fri, 02 Oct 2010 15:50:12 GMT" (RFC1123)
            if (DateTimeOffset.TryParseExact(request.Headers["If-Modified-Since"], "R", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out ifModifiedSince)) {
                if (resourceLastModified.ToUniversalTime().TrimMilliseconds() <= ifModifiedSince.ToUniversalTime()) {
                    return true;
                }
            }
            return false;
        }
    }
}

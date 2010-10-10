using System;
using System.Web;

namespace NuPack.Server {
    public static class HttpContextExtensions {
        public static bool IsUnmodified(this HttpRequestBase request, DateTime resourceLastModified) {
            DateTime ifModifiedSince;
            if (DateTime.TryParse(request.Headers["If-Modified-Since"], out ifModifiedSince)) {
                if (resourceLastModified.ToUniversalTime().TrimToSeconds() <= ifModifiedSince.ToUniversalTime()) {
                    return true;
                }
            }
            return false;
        }
    }
}
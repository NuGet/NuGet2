using System;
using System.Web;

namespace NuPack.Server {
    public static class HttpContextExtensions {
        public static bool IsUnmodified(this HttpRequestBase request, DateTimeOffset resourceLastModified) {
            DateTimeOffset ifModifiedSince;
            
            if (DateTimeOffset.TryParse(request.Headers["If-Modified-Since"], out ifModifiedSince)) {
                if (resourceLastModified.ToUniversalTime() <= ifModifiedSince.ToUniversalTime()) {
                    return true;
                }
            }
            return false;
        }
    }
}
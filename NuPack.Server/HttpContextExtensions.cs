using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NuPack.Server {
    public static class HttpContextExtensions {
        public static void EnableOutputCache(this HttpContextBase context, TimeSpan duration) {
            // Output caching for 30 minutes
            HttpCachePolicyBase cachePolicy = context.Response.Cache;
            cachePolicy.SetCacheability(HttpCacheability.Public);
            cachePolicy.SetExpires(context.Timestamp.Add(duration));
            cachePolicy.SetValidUntilExpires(true);
            cachePolicy.SetLastModified(context.Timestamp);
        }
    }
}
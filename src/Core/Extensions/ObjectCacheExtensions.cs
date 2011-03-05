using System;
using System.Runtime.Caching;

namespace NuGet {
    internal static class ObjectCacheExtensions {
        public static T GetOrAdd<T>(this ObjectCache cache, string cacheKey, Func<T> factory, TimeSpan slidingExpiration) where T : class {
            var value = (T)cache.Get(cacheKey);
            if (value == null) {
                value = factory();
                var policy = new CacheItemPolicy();
                policy.SlidingExpiration = slidingExpiration;
                cache.Add(cacheKey, value, policy);
            }
            return value;
        }
    }
}

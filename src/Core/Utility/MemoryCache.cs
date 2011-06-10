using System;
using System.Collections.Concurrent;

namespace NuGet
{
    internal sealed class MemoryCache
    {
        private MemoryCache()
        {
        }

        internal static MemoryCache Default
        {
            get
            {
                return InternalMemoryCache.Instance;
            }
        }

        private ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();

        internal T GetOrAdd<T>(string cacheKey, Func<T> factory, TimeSpan slidingExpiration) where T : class
        {
            CacheItem result;
            if (!_cache.TryGetValue(cacheKey, out result) || result.Expiry < DateTime.Now)
            {
                result = new CacheItem { Item = factory(), Expiry = DateTime.Now.Add(slidingExpiration) };
                _cache[cacheKey] = result;
            }
            return (T)result.Item;
        }

        internal void Remove(string cacheKey)
        {
            CacheItem item;
            _cache.TryRemove(cacheKey, out item);
        }

        private class CacheItem
        {
            public object Item { get; set; }
            public DateTime Expiry { get; set; }
        }

        private class InternalMemoryCache
        {
            static InternalMemoryCache()
            {
            }

            internal static MemoryCache Instance = new MemoryCache();
        }
    }
}

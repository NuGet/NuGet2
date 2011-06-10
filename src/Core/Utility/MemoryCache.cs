using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace NuGet {
    internal sealed class MemoryCache {
        private MemoryCache() {
        }

        internal static MemoryCache Default {
            get {
                return InternalMemoryCache.Instance;
            }
        }

        private ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();

        internal int Count { get { return _cache.Count; } }

        internal T GetOrAdd<T>(string cacheKey, Func<T> factory, TimeSpan slidingExpiration) where T : class {
            CacheItem result;
            if (!_cache.TryGetValue(cacheKey, out result)) {
                result = new CacheItem(this, cacheKey, factory(), slidingExpiration);
                _cache[cacheKey] = result;
            }
            return (T)result.Item;
        }

        internal void Remove(string cacheKey) {
            CacheItem item;
            _cache.TryRemove(cacheKey, out item);
        }

        private class CacheItem {
            public CacheItem(MemoryCache owner, string cacheKey, object item, TimeSpan expiry) {
                _item = item;

                Task.Factory.StartNew(() => {
                    Thread.Sleep(expiry);
                    owner.Remove(cacheKey);
                });
            }

            private object _item;
            public object Item {
                get {
                    return _item;
                }
            }
        }

        private class InternalMemoryCache {
            static InternalMemoryCache() {
            }

            internal static MemoryCache Instance = new MemoryCache();
        }
    }
}

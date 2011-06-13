using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet {
    internal sealed class MemoryCache {
        private static readonly Lazy<MemoryCache> _default = new Lazy<MemoryCache>(() => new MemoryCache());
        private static readonly TimeSpan _gcInterval = TimeSpan.FromSeconds(10);

        private readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();

        private MemoryCache() {
            StartGCTask();
        }

        internal static MemoryCache Default {
            get {
                return _default.Value;
            }
        }

        internal T GetOrAdd<T>(string cacheKey, Func<T> factory, TimeSpan slidingExpiration) where T : class {
            CacheItem cachedItem;
            if (!_cache.TryGetValue(cacheKey, out cachedItem) || cachedItem.Expired) {
                // Recreate the item if it's expired or doesn't exit
                cachedItem = new CacheItem(factory());
                _cache.TryAdd(cacheKey, cachedItem);
            }
            // Increase the expiration time
            cachedItem.Expires = DateTime.Now + slidingExpiration;
            return (T)cachedItem.Value;
        }

        internal void Remove(string cacheKey) {
            CacheItem item;
            _cache.TryRemove(cacheKey, out item);
        }

        private void StartGCTask() {
            Task.Factory.StartNew(GarbageCollectExpiredEntries, TaskCreationOptions.LongRunning);
        }

        private void GarbageCollectExpiredEntries() {
            while (true) {
                // Take a snapshot of the entries
                var entries = _cache.ToList();

                // Remove all the expired ones
                foreach (var entry in entries) {
                    if (entry.Value.Expired) {
                        Remove(entry.Key);
                    }
                }

                Thread.Sleep(_gcInterval);
            }
        }

        private class CacheItem {
            private readonly object _value;
            
            public CacheItem(object value) {
                _value = value;
            }

            public object Value {
                get {
                    return _value;
                }
            }

            public DateTime Expires { get; set; }

            public bool Expired {
                get {
                    return DateTime.Now > Expires;
                }
            }
        }
    }
}

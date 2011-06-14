using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace NuGet {
    internal sealed class MemoryCache : IDisposable {
        private static readonly Lazy<MemoryCache> _instance = new Lazy<MemoryCache>(() => new MemoryCache());
        // Interval to wait before cleaning up expired items
        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        // Cache keys are case-sensitive
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();
        private readonly Timer _timer;

        private MemoryCache() {
            _timer = new Timer(RemoveExpiredEntries, null, _cleanupInterval, _cleanupInterval);
        }

        internal static MemoryCache Instance {
            get {
                return _instance.Value;
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
            cachedItem.UpdateUsage(slidingExpiration);

            return (T)cachedItem.Value;
        }

        internal void Remove(string cacheKey) {
            CacheItem item;
            _cache.TryRemove(cacheKey, out item);
        }

        private void RemoveExpiredEntries(object state) {
            // Take a snapshot of the entries
            var entries = _cache.ToList();

            // Remove all the expired ones
            foreach (var entry in entries) {
                if (entry.Value.Expired) {
                    Remove(entry.Key);
                }
            }
        }

        void IDisposable.Dispose() {
            if (_timer != null) {
                _timer.Dispose();
            }
        }

        private class CacheItem {
            private readonly object _value;
            private long _expires;

            public CacheItem(object value) {
                _value = value;
            }

            public object Value {
                get {
                    return _value;
                }
            }

            public void UpdateUsage(TimeSpan slidingExpiration) {
                _expires = (DateTime.UtcNow + slidingExpiration).Ticks;
            }

            public bool Expired {
                get {
                    long ticks = DateTime.UtcNow.Ticks;
                    long expires = Interlocked.Read(ref _expires);
                    // > is atomic on primitive types
                    return ticks > expires;
                }
            }
        }
    }
}

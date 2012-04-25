using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace NuGet
{
    public sealed class MemoryCache : IDisposable
    {
        private static readonly Lazy<MemoryCache> _instance = new Lazy<MemoryCache>(() => new MemoryCache());
        // Interval to wait before cleaning up expired items
        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        // Cache keys are case-sensitive
        private readonly ConcurrentDictionary<object, CacheItem> _cache = new ConcurrentDictionary<object, CacheItem>();
        private readonly Timer _timer;

        internal MemoryCache()
        {
            _timer = new Timer(RemoveExpiredEntries, null, _cleanupInterval, _cleanupInterval);
        }

        internal static MemoryCache Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        internal T GetOrAdd<T>(object cacheKey, Func<T> factory, TimeSpan slidingExpiration) where T : class
        {
            // Although this method would return values that have expired while also elavating them to unexpired entries,
            // none of the data that we cache is time sensitive. At worst, an item will be cached for an extra _cleanupInterval duration.

            CacheItem cacheFactory = new CacheItem(factory);

            var cachedItem = _cache.GetOrAdd(cacheKey, cacheFactory);

            // Increase the expiration time
            cachedItem.UpdateUsage(slidingExpiration);

            return (T)cachedItem.Value;
        }

        internal void Remove(object cacheKey)
        {
            CacheItem item;
            _cache.TryRemove(cacheKey, out item);
        }

        private void RemoveExpiredEntries(object state)
        {
            // Take a snapshot of the entries
            var entries = _cache.ToList();

            // Remove all the expired ones
            foreach (var entry in entries)
            {
                if (entry.Value != null && entry.Value.Expired)
                {
                    Remove(entry.Key);
                }
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }
        }

        private sealed class CacheItem
        {
            private readonly Lazy<object> _valueFactory;
            private long _expires;

            public CacheItem(Func<object> valueFactory)
            {
                _valueFactory = new Lazy<object>(valueFactory);
            }

            public object Value
            {
                get
                {
                    return _valueFactory.Value;
                }
            }

            public void UpdateUsage(TimeSpan slidingExpiration)
            {
                _expires = DateTime.UtcNow.Ticks + slidingExpiration.Ticks;
            }

            public bool Expired
            {
                get
                {
                    long ticks = DateTime.UtcNow.Ticks;
                    long expires = Interlocked.Read(ref _expires);
                    // > is atomic on primitive types
                    return ticks > expires;
                }
            }
        }
    }
}

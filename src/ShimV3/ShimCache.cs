using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.ShimV3
{
    public class ShimCache : IShimCache
    {
        private ConcurrentDictionary<string, CacheItem> _cache;
        private Timer _timer;
        private TimeSpan _expires;
        private TimeSpan _cleanup;

        public ShimCache()
        {
            _expires = new TimeSpan(0, 5, 0);
            _cleanup = new TimeSpan(0, 0, 30);
            _cache = new ConcurrentDictionary<string, CacheItem>();
            _timer = new Timer(TimeTick, null, 0, 0);
        }

        public void AddOrUpdate(Uri uri, JObject blob)
        {
            _cache.AddOrUpdate(uri.ToString().ToLowerInvariant(), new CacheItem(blob), (k, i) => new CacheItem(blob));

            _timer.Change((int)_cleanup.TotalMilliseconds, (int)_cleanup.TotalMilliseconds);
        }

        public bool TryGet(Uri uri, out JObject blob)
        {
            CacheItem item = null;
            if (_cache.TryGetValue(uri.ToString().ToLowerInvariant(), out item))
            {
                blob = item.Value;
                return true;
            }

            blob = null;
            return false;
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void TimeTick(object state)
        {
            // clean up
            _cache.RemoveAll(p => DateTime.Now.Subtract(p.Value.Added).TotalMinutes > _expires.TotalMinutes);
        }

        private class CacheItem
        {
            public DateTime Added { get; private set; }
            public JObject Value { get; private set; }

            public CacheItem(JObject value)
            {
                Added = DateTime.Now;
                Value = value;
            }
        }
    }
}

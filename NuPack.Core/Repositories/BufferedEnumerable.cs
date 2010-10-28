using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NuGet {
    /// <summary>
    /// An IEnumerble&lt;T&gt; implementation that queries an IQueryable&lt;T&gt; on demand. 
    /// This is usefult when alot of data can be returned from an IQueryable source, but
    /// you don't want to do it all at once.
    /// </summary>
    public class BufferedEnumerable<TElement> : IEnumerable<TElement> {
        private readonly IQueryable<TElement> _source;
        private readonly int _bufferSize;

        public BufferedEnumerable(IQueryable<TElement> source, int bufferSize) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            _source = source;
            _bufferSize = bufferSize;
        }

        public IEnumerator<TElement> GetEnumerator() {
            return new BufferedEnumerator<TElement>(_source, _bufferSize);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public override string ToString() {
            return _source.ToString();
        }

        private class BufferedEnumerator<T> : IEnumerator<T> {
            private readonly int _bufferSize;
            private IQueryable<T> _source;
            private List<T> _cache;
            private bool _sourceEmpty;
            private int _index = -1;

            public BufferedEnumerator(IQueryable<T> source, int bufferSize) {
                _source = source;
                _bufferSize = bufferSize;
            }

            public T Current {
                get {
                    if (_index < _cache.Count) {
                        return _cache[_index];
                    }
                    // REVIEW: throw?
                    return default(T);
                }
            }

            private bool NeedMore {
                get {
                    return _cache == null || (_index == _cache.Count - 1);
                }
            }

            public void Dispose() {
                _source = null;
                _index = -1;
                _cache = null;
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public bool MoveNext() {
                if (NeedMore) {
                    // If the source is empty then bail
                    if (_sourceEmpty) {
                        return false;
                    }

                    // See if we need to query again
                    // If this is the first query then initialize the local cache
                    if (_cache == null) {
                        _cache = new List<T>(_bufferSize);
                    }

                    // Execute a query
                    IList<T> items = _source.Skip(_cache.Count).Take(_bufferSize).ToList();

                    // No items in the original query then bail
                    if (!items.Any()) {
                        return false;
                    }

                    _sourceEmpty = items.Count < _bufferSize;

                    // Update our cache
                    _cache.AddRange(items);
                }

                _index++;
                return true;
            }

            public void Reset() {
                _sourceEmpty = false;
                _cache = null;
                _index = -1;
            }

            public override string ToString() {
                return _source.ToString();
            }
        }
    }    
}

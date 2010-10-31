using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet {
    /// <summary>
    /// An IEnumerble&lt;T&gt; implementation that queries an IQueryable&lt;T&gt; on demand. 
    /// This is usefult when alot of data can be returned from an IQueryable source, but
    /// you don't want to do it all at once.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Collection isn't correct")]
    public class BufferedEnumerable<TElement> : IEnumerable<TElement> {
        private readonly IQueryable<TElement> _source;
        private readonly int _bufferSize;
        private readonly List<TElement> _cache;

        public BufferedEnumerable(IQueryable<TElement> source, int bufferSize) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            _cache = new List<TElement>(bufferSize);
            _source = source;
            _bufferSize = bufferSize;
        }

        public IEnumerator<TElement> GetEnumerator() {
            return new BufferedEnumerator<TElement>(_cache, _source, _bufferSize);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public override string ToString() {
            return _source.ToString();
        }

        internal class BufferedEnumerator<T> : IEnumerator<T> {
            private readonly int _bufferSize;

            private IQueryable<T> _source;
            private List<T> _cache;
            private bool _hasItems = true;
            private int _index = -1;

            public BufferedEnumerator(List<T> cache, IQueryable<T> source, int bufferSize) {
                _cache = cache;
                _source = source;
                _bufferSize = bufferSize;
            }

            public T Current {
                get {
                    Debug.Assert(_index < _cache.Count);
                    return _cache[_index];
                }
            }

            internal bool IsEmpty {
                get {
                    return _hasItems && (_index == _cache.Count - 1);
                }
            }

            public void Dispose() {
                _source = null;
                _cache = null;
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public bool MoveNext() {
                if (IsEmpty) {
                    // Request a page
                    List<T> items = _source.Skip(_cache.Count)
                                           .Take(_bufferSize)
                                           .ToList();

                    // See if we have anymore items after the last query
                    _hasItems = _bufferSize == items.Count;

                    // Add it to the cache
                    _cache.AddRange(items);
                }

                _index++;
                // We can keep going unless the source said we're empty
                return _index < _cache.Count;
            }

            public void Reset() {
                _index = -1;
            }

            public override string ToString() {
                return _source.ToString();
            }
        }
    }
}

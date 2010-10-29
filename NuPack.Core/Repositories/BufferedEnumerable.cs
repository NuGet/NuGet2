using System;
using System.Collections;
using System.Collections.Generic;
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
            private IEnumerator<T> _cachedEnumerator;
            private bool _hasItems = true;
            private int _cached = 0;

            public BufferedEnumerator(IQueryable<T> source, int bufferSize) {
                _source = source;
                _bufferSize = bufferSize;
            }

            public T Current {
                get {
                    return _cachedEnumerator.Current;
                }
            }

            private bool Empty {
                get {
                    return _hasItems && (_cachedEnumerator == null || !_cachedEnumerator.MoveNext());
                }
            }

            public void Dispose() {
                _source = null;
                _cachedEnumerator.Dispose();
                _cachedEnumerator = null;
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public bool MoveNext() {               
                if (Empty) {
                    // Request a page
                    _cachedEnumerator = _source.Skip(_cached).Take(_bufferSize).GetEnumerator();
                   
                    // Increment the amount of items cached
                    _cached += _bufferSize;

                    // See if we have anymore items after the last query
                    _hasItems = _cachedEnumerator.MoveNext();
                }

                // We can keep going unless the source said we're empty
                return _hasItems;
            }

            public void Reset() {
                _cached = 0;
                _hasItems = true;
                _cachedEnumerator.Reset();
            }

            public override string ToString() {
                return _source.ToString();
            }
        }
    }
}

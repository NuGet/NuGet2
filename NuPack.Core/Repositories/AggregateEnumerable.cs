using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace NuGet {
    internal class AggregateEnumerable<TElement> : IEnumerable<TElement> {
        private readonly IEnumerable<IEnumerator<TElement>> _subQueries;
        private readonly IEqualityComparer<TElement> _equlityComparer;
        private readonly IComparer<TElement> _comparer;

        public AggregateEnumerable(IEnumerable<IEnumerator<TElement>> subQueries,
                                   IEqualityComparer<TElement> equlityComparer,
                                   IComparer<TElement> comparer) {
            _subQueries = subQueries;
            _equlityComparer = equlityComparer;
            _comparer = comparer;
        }

        public IEnumerator<TElement> GetEnumerator() {
            return new AggregateEnumerator<TElement>(_subQueries, _equlityComparer, _comparer);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        private class AggregateEnumerator<T> : IEnumerator<T> {
            private IEnumerable<IEnumerator<T>> _subQueries;
            private IEqualityComparer<T> _equalityComparer;
            private PriorityQueue<T, T> _queue;            

            public AggregateEnumerator(IEnumerable<IEnumerator<T>> subQueries,
                                       IEqualityComparer<T> equalityComparer,
                                       IComparer<T> comparer) {
                _subQueries = subQueries;
                _equalityComparer = equalityComparer;
                _queue = new PriorityQueue<T, T>(comparer);
            }

            public T Current {
                get;
                private set;
            }

            public void Dispose() {
                foreach (var query in _subQueries) {
                    query.Dispose();
                }
                _subQueries = null;
                _equalityComparer = null;
                _queue = null;
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public bool MoveNext() {
                // Check each sub queries' enumerator and if there is more then add it to the queue
                foreach (var query in _subQueries) {
                    if (query.MoveNext()) {
                        _queue.Enqueue(query.Current, query.Current);
                    }
                }

                // Remove duplicates if necessary
                while (!_queue.IsEmpty) {
                    T newElement = _queue.Dequeue();
                    if (Current == null || !_equalityComparer.Equals(Current, newElement)) {
                        Current = newElement;
                        break;
                    }
                }

                return !_queue.IsEmpty;
            }

            public void Reset() {
                foreach (var query in _subQueries) {
                    query.Reset();
                }

                Current = default(T);
                _queue.Clear();
            }

            // Small priority queue class
            private class PriorityQueue<P, V> {
                private SortedDictionary<P, Queue<V>> _list;

                public PriorityQueue(IComparer<P> comparer) {
                    _list = new SortedDictionary<P, Queue<V>>(comparer);
                }

                public void Enqueue(P priority, V value) {
                    Queue<V> queue;
                    if (!_list.TryGetValue(priority, out queue)) {
                        queue = new Queue<V>();
                        _list.Add(priority, queue);
                    }
                    queue.Enqueue(value);
                }

                public V Dequeue() {
                    // will throw if there isn’t any first element!
                    var pair = _list.First();
                    var v = pair.Value.Dequeue();
                    if (pair.Value.Count == 0) {// nothing left of the top priority.
                        _list.Remove(pair.Key);
                    }
                    return v;
                }

                public bool IsEmpty {
                    get { return !_list.Any(); }
                }

                public void Clear() {
                    _list.Clear();
                }
            }
        }
    }
}

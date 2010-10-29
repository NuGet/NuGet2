using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NuGet {
    internal class AggregateEnumerable<TElement> : IEnumerable<TElement> {
        private readonly IEnumerable<IEnumerable<TElement>> _subQueries;
        private readonly IEqualityComparer<TElement> _equlityComparer;
        private readonly IComparer<TElement> _comparer;

        public AggregateEnumerable(IEnumerable<IEnumerable<TElement>> subQueries,
                                   IEqualityComparer<TElement> equlityComparer,
                                   IComparer<TElement> comparer) {
            _subQueries = subQueries;
            _equlityComparer = equlityComparer;
            _comparer = comparer;
        }

        public IEnumerator<TElement> GetEnumerator() {
            return new AggregateEnumerator<TElement>(_subQueries.Select(q => q.GetEnumerator()).ToList(), 
                                                     _equlityComparer, 
                                                     _comparer);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        private class AggregateEnumerator<T> : IEnumerator<T> {
            private IEnumerable<IEnumerator<T>> _subQueries;
            private IEqualityComparer<T> _equalityComparer;
            private PriorityQueue<T> _queue;

            public AggregateEnumerator(IEnumerable<IEnumerator<T>> subQueries,
                                       IEqualityComparer<T> equalityComparer,
                                       IComparer<T> comparer) {
                _subQueries = subQueries;
                _equalityComparer = equalityComparer;
                _queue = new PriorityQueue<T>(comparer);
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
                do {
                    // TODO: Execute move next in parallel?
                    
                    // Check each sub queries' enumerator and if there is more then add it to the queue
                    // in priority order
                    foreach (var query in _subQueries) {
                        if (query.MoveNext()) {
                            _queue.Enqueue(query.Current);
                        }
                    }

                    if (!_queue.IsEmpty) {
                        Current = _queue.DequeueMin();
                        return true;
                    }
                }
                while (!_queue.IsEmpty);

                return false;
            }

            public void Reset() {
                foreach (var query in _subQueries) {
                    query.Reset();
                }

                Current = default(T);
                _queue.Clear();
            }

            // Small priority queue class
            private class PriorityQueue<TValue> {
                private SortedSet<TValue> _set;

                public PriorityQueue(IComparer<TValue> comparer) {
                    _set = new SortedSet<TValue>(comparer);
                }

                public void Enqueue(TValue value) {
                    _set.Add(value);
                }

                public TValue DequeueMin() {
                    TValue min = _set.Min;
                    _set.Remove(min);
                    return min;
                }

                public bool IsEmpty {
                    get {
                        return !_set.Any();
                    }
                }

                public void Clear() {
                    _set.Clear();
                }
            }
        }
    }
}

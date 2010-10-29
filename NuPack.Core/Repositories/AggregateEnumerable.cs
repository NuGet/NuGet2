using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
                    // Check each sub queries' enumerator and if there is more then add it to the queue
                    // in priority order
                    foreach (var query in _subQueries) {
                        if (query.MoveNext()) {
                            _queue.Enqueue(query.Current);
                        }
                    }

                    if (!_queue.IsEmpty) {
                        // Remove duplicates
                        T nextElement = _queue.Dequeue();
                        if (Current == null || !_equalityComparer.Equals(Current, nextElement)) {
                            // When we find the an element that's not a duplicate of the current
                            // return it and move on
                            Current = nextElement;
                            return true;
                        }
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
                private SortedDictionary<TValue, Queue<TValue>> _list;

                public PriorityQueue(IComparer<TValue> comparer) {
                    _list = new SortedDictionary<TValue, Queue<TValue>>(comparer);
                }

                public void Enqueue(TValue value) {
                    Queue<TValue> queue;
                    if (!_list.TryGetValue(value, out queue)) {
                        queue = new Queue<TValue>();
                        _list.Add(value, queue);
                    }
                    queue.Enqueue(value);
                }

                public TValue Dequeue() {
                    // Will throw if there isn’t any first element!
                    KeyValuePair<TValue, Queue<TValue>> pair = _list.First();
                    TValue value = pair.Value.Dequeue();

                    // Nothing left of the top priority.
                    if (pair.Value.Count == 0) {
                        _list.Remove(pair.Key);
                    }
                    return value;
                }

                public bool IsEmpty {
                    get {
                        return !_list.Any();
                    }
                }

                public void Clear() {
                    _list.Clear();
                }
            }
        }
    }
}

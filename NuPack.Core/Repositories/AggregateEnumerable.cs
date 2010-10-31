using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuGet {
    internal class AggregateEnumerable<TElement> : IEnumerable<TElement> {
        private readonly IEnumerable<IEnumerable<TElement>> _subQueries;
        private readonly IEqualityComparer<TElement> _equalityComparer;
        private readonly IComparer<TElement> _comparer;

        public AggregateEnumerable(IEnumerable<IEnumerable<TElement>> subQueries,
                                   IEqualityComparer<TElement> equalityComparer,
                                   IComparer<TElement> comparer) {
            _subQueries = subQueries;
            _equalityComparer = equalityComparer;
            _comparer = comparer;
        }

        public IEnumerator<TElement> GetEnumerator() {
            return new AggregateEnumerator<TElement>(_subQueries.Select(q => q.GetEnumerator()).ToList(),
                                                     _equalityComparer,
                                                     _comparer);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        private class AggregateEnumerator<T> : IEnumerator<T> {
            private IEnumerable<IEnumerator<T>> _subQueries;
            private PriorityQueue<T> _queue;

            public AggregateEnumerator(IEnumerable<IEnumerator<T>> subQueries,
                                       IEqualityComparer<T> equalityComparer,
                                       IComparer<T> comparer) {
                _subQueries = subQueries;
                _queue = new PriorityQueue<T>(comparer, equalityComparer);
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
                _queue = null;
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public bool MoveNext() {
                do {
                    // Run tasks in parallel
                    var subQueryTasks = (from q in _subQueries
                                         select Task.Factory.StartNew(
                                                             () => q.MoveNext() ?
                                                                   new {
                                                                       Empty = false,
                                                                       Value = q.Current
                                                                   }
                                                                   :
                                                                   new {
                                                                       Empty = true,
                                                                       Value = default(T)
                                                                   }
                                                             )
                                         ).ToArray();

                    // Wait for everything to complete
                    Task.WaitAll(subQueryTasks);

                    // Check each sub queries' enumerator and if there is more then add it to the queue
                    // in priority order
                    foreach (var task in subQueryTasks) {
                        if (!task.Result.Empty) {
                            _queue.Enqueue(task.Result.Value);
                        }
                    }

                    if (!_queue.IsEmpty) {
                        Current = _queue.Dequeue();
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

            private class PriorityQueue<TValue> {
                private readonly SortedDictionary<TValue, HashSet<TValue>> _lookup;
                private readonly IEqualityComparer<TValue> _equalityComparer;

                public PriorityQueue(IComparer<TValue> comparer,
                                     IEqualityComparer<TValue> equalityComparer) {
                    _equalityComparer = equalityComparer;
                    _lookup = new SortedDictionary<TValue, HashSet<TValue>>(comparer);
                }

                public void Enqueue(TValue value) {
                    HashSet<TValue> queue;
                    if (!_lookup.TryGetValue(value, out queue)) {
                        queue = new HashSet<TValue>(_equalityComparer);
                        _lookup.Add(value, queue);
                    }
                    queue.Add(value);
                }

                public TValue Dequeue() {
                    // Will throw if there isn't any first element!
                    var pair = _lookup.First();
                    var value = pair.Value.First();

                    // Remove the item from the set
                    pair.Value.Remove(value);

                    // Nothing left of the top priority
                    if (pair.Value.Count == 0) {
                        _lookup.Remove(pair.Key);
                    }
                    return value;
                }

                public bool IsEmpty {
                    get {
                        return !_lookup.Any();
                    }
                }

                public void Clear() {
                    _lookup.Clear();
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet {
    internal class PriorityQueue<TValue> {
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

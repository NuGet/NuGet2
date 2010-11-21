using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet {
    internal class PriorityQueue<TValue> {
        private readonly SortedDictionary<TValue, Queue<TValue>> _lookup;
        
        public PriorityQueue(IComparer<TValue> comparer) {
            _lookup = new SortedDictionary<TValue, Queue<TValue>>(comparer);
        }

        public void Enqueue(TValue value) {
            Queue<TValue> queue;
            if (!_lookup.TryGetValue(value, out queue)) {
                queue = new Queue<TValue>();
                _lookup.Add(value, queue);
            }
            queue.Enqueue(value);
        }

        public TValue Dequeue() {
            // Will throw if there isn't any first element!
            var pair = _lookup.First();
            var value = pair.Value.Dequeue();

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

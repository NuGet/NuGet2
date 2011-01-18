using System.Collections.Generic;

namespace NuGet {
    internal class LazyQueue<T> {
        private readonly IEnumerator<T> _enumerator;
        private T _peeked;

        public LazyQueue(IEnumerator<T> enumerator) {
            _enumerator = enumerator;
        }

        public bool TryPeek(out T element) {
            element = default(T);

            if (_peeked != null) {
                element = _peeked;
                return true;
            }

            bool next = _enumerator.MoveNext();

            if (next) {
                element = _enumerator.Current;
                _peeked = element;
            }

            return next;
        }

        public void Dequeue() {
            // Reset the peeked element
            _peeked = default(T);
        }
    }
}

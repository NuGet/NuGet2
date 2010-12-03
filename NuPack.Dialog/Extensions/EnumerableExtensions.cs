using System.Collections.Generic;

namespace NuGet.Dialog.Extensions {
    internal static class EnumerableExtensions {
        /// <summary>
        /// Returns a distinct set of elements using the comparer specified. This implementation will pick the last occurence
        /// of each element instead of picking the first. This method assumes that similar items occur in order.
        /// </summary>
        internal static IEnumerable<TElement> DistinctLast<TElement>(this IEnumerable<TElement> source,
                                                                   IEqualityComparer<TElement> comparer) {
            bool first = true;
            var lastElement = default(TElement);

            foreach (TElement element in source) {
                if (!first && !comparer.Equals(element, lastElement)) {                   
                    yield return lastElement;                   
                }

                lastElement = element;
                first = false;
            }

            if (!first) {
                yield return lastElement;
            }
        }
    }
}
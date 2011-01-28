using System.Collections.Generic;

namespace NuGet.VisualStudio {
    public static class EnumerableExtensions {
        /// <summary>
        /// Returns a distinct set of elements using the comparer specified. This implementation will pick the last occurence
        /// of each element instead of picking the first. This method assumes that similar items occur in order.
        /// </summary>
        public static IEnumerable<TElement> DistinctLast<TElement>(this IEnumerable<TElement> source,
                                                                   IEqualityComparer<TElement> comparer) {
            bool first = true;
            var previousElement = default(TElement);

            foreach (TElement element in source) {
                if (!first && !comparer.Equals(element, previousElement)) {                   
                    yield return previousElement;                   
                }

                previousElement = element;
                first = false;
            }

            if (!first) {
                yield return previousElement;
            }
        }
    }
}
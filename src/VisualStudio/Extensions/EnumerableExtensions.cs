using System.Collections.Generic;

namespace NuGet.VisualStudio {
    public static class EnumerableExtensions {        
        /// <summary>
        /// Returns a distinct set of elements using the comparer specified. This implementation will pick the last occurence
        /// of each element instead of picking the first. This method assumes that similar items occur in order.
        /// </summary>        
        public static IEnumerable<TElement> DistinctLast<TElement>(this IEnumerable<TElement> source,
                                                                   IEqualityComparer<TElement> equalityComparer,
                                                                   IComparer<TElement> comparer) {
            bool first = true;
            bool maxElementHasValue = false;
            var previousElement = default(TElement);
            var maxElement = default(TElement);

            foreach (TElement element in source) {
                // If we're starting a new group then return the max element from the last group
                if (!first && !equalityComparer.Equals(element, previousElement)) {
                    yield return maxElement;

                    // Reset the max element
                    maxElementHasValue = false;
                }

                // If the current max element has a value and is bigger or doesn't have a value then update the max
                if (!maxElementHasValue || (maxElementHasValue && comparer.Compare(maxElement, element) < 0)) {
                    maxElement = element;
                    maxElementHasValue = true;
                }

                previousElement = element;
                first = false;
            }

            if (!first) {
                yield return maxElement;
            }
        }
    }
}
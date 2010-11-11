using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet {
    public static class EnumerableExtensions {
        /// <summary>
        /// The purpose of this method is to mitigate a partial trust issue. We expose
        /// EnumerableQuery (wrapping an enumrable in an IQueryable) throughout the codebase
        /// and expression compilation doesn't work in some cases. See SafeEnumerableQuery for more details.
        /// </summary>
        public static IQueryable<T> AsSafeQueryable<T>(this IEnumerable<T> source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            return new SafeEnumerableQuery<T>(source);
        }
    }
}

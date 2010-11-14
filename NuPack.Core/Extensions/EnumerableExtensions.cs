using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NuGet {
    internal static class EnumerableExtensions {
        private static readonly bool _isRewritingRequired = IsRewritingRequired();

        /// <summary>
        /// The purpose of this method is to mitigate a partial trust issue. We expose
        /// EnumerableQuery (wrapping an enumrable in an IQueryable) throughout the codebase
        /// and expression compilation doesn't work in some cases. See SafeEnumerableQuery for more details.
        /// </summary>
        internal static IQueryable<T> AsSafeQueryable<T>(this IEnumerable<T> source) {
            return AsSafeQueryable(source, rewriteQuery: _isRewritingRequired);
        }

        internal static IQueryable<T> AsSafeQueryable<T>(this IEnumerable<T> source, bool rewriteQuery) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (rewriteQuery) {
                return new SafeEnumerableQuery<T>(source);
            }
            // AsQueryable returns the original source if it is already a IQueryable<T>. 
            return source.AsQueryable();
        }

        /// <summary>
        /// Replacing closures with constant values is required only when executing in partial trust and the NuGet assembly is GACed.
        /// </summary>
        private static bool IsRewritingRequired() {
            AppDomain appDomain = AppDomain.CurrentDomain;
            Assembly assembly = typeof(EnumerableExtensions).Assembly; ;
            return appDomain.IsHomogenous && !appDomain.IsFullyTrusted && assembly.IsFullyTrusted;
        }
    }
}

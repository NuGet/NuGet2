using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio {

    /// <summary>
    /// This method attempts to retrieve all elements from the specified IQueryable, taking into account 
    /// the fact that the IQueryable may come from OData service feed, which imposes a server-side paging 
    /// limits in its query responses.
    /// 
    /// It works around the limitation by repeatedly querying the source until the latter runs out of items.
    /// </summary>
    public static class QueryExtensions {

        public static IEnumerable<T> GetAll<T>(this IQueryable<T> source, int skip, int? first) {
            bool useFirst = first.HasValue;

            int totalItemCount = 0;
            while (!useFirst || totalItemCount < first.Value) {
                var query = source.Skip(skip + totalItemCount);
                if (useFirst) {
                    // only take what we need
                    query = query.Take(first.Value - totalItemCount);
                }

                int queryItemCount = 0;
                foreach (T item in query) {
                    yield return item;
                    queryItemCount++;
                }

                totalItemCount += queryItemCount;

                if (queryItemCount == 0) {
                    // stop if no item is returned
                    yield break;
                }
            }
        }

        public static IEnumerable<T> SkipAndTake<T>(this IEnumerable<T> source, int skip, int? take) {
            var queryableSource = source as IQueryable<T>;
            if (queryableSource != null) {
                return GetAll(queryableSource, skip, take);
            }
            else {
                var query = source.Skip(skip);
                if (take.HasValue) {
                    query = query.Take(take.Value);
                }
                return query;
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NuGet {
    internal class AggregateQuery<T> : IQueryable<T>, IQueryProvider, IOrderedQueryable<T> {
        private const int QueryCacheSize = 30;

        private readonly IEnumerable<IQueryable<T>> _queryables;
        private readonly Expression _expression;
        private readonly IEqualityComparer<T> _equalityComparer;
        private readonly IList<IEnumerable<T>> _subQueries;

        public AggregateQuery(IEnumerable<IQueryable<T>> queryables, IEqualityComparer<T> equalityComparer) {
            _queryables = queryables;
            _equalityComparer = equalityComparer;
            _expression = Expression.Constant(this);
            _subQueries = GetSubQueries(_expression);
        }

        private AggregateQuery(IEnumerable<IQueryable<T>> queryables,
                               IEqualityComparer<T> equalityComparer,
                               IList<IEnumerable<T>> subQueries,
                               Expression expression) {
            _queryables = queryables;
            _equalityComparer = equalityComparer;
            _expression = expression;
            _subQueries = subQueries;
        }

        public IEnumerator<T> GetEnumerator() {
            // Rewrite the expression for aggregation i.e. remove things that don't make sense to apply
            // after all initial expression has been applied.
            var aggregateQuery = GetAggregateEnumerable().AsSafeQueryable();

            Expression aggregateExpression = RewriteForAggregation(aggregateQuery, Expression);
            return aggregateQuery.Provider.CreateQuery<T>(aggregateExpression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public Type ElementType {
            get {
                return typeof(T);
            }
        }

        public Expression Expression {
            get {
                return _expression;
            }
        }

        public IQueryProvider Provider {
            get {
                return this;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) {
            return (IQueryable<TElement>)CreateQuery(typeof(TElement), expression);
        }

        public IQueryable CreateQuery(Expression expression) {
            // Copied logic from EnumerableQuery
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }

            Type elementType = QueryableUtility.FindGenericType(typeof(IQueryable<>), expression.Type);

            if (elementType == null) {
                throw new ArgumentException(String.Empty, "expression");
            }

            return CreateQuery(elementType, expression);
        }

        public TResult Execute<TResult>(Expression expression) {
            var results = (from queryable in _queryables
                           select Execute<TResult>(queryable, expression)).AsSafeQueryable();

            if (QueryableUtility.IsQueryableMethod(expression, "Count")) {
                // HACK: This is in correct since we aren't removing duplicates but count is mostly for paging
                // so we don't care *that* much
                return (TResult)(object)results.Cast<int>().Sum();
            }

            return Execute<TResult>(results, expression);
        }

        public object Execute(Expression expression) {
            return Execute<object>(expression);
        }

        private IEnumerable<T> GetAggregateEnumerable() {
            // Used to pick the right element from each sub query in the right order
            var comparer = new OrderingComparer<T>(Expression);

            // Create lazy queues over each sub query so we can lazily pull items from it
            var lazyQueues = _subQueries.Select(query => new LazyQueue<T>(query.GetEnumerator())).ToList();

            // Used to keep track of everything we've seen so far (we never show duplicates)
            var seen = new HashSet<T>(_equalityComparer);

            do {                
                T minElement = default(T);
                LazyQueue<T> minQueue = null;

                // Run tasks in parallel
                var tasks = (from queue in lazyQueues
                             select Task.Factory.StartNew(() => {
                                 T current;
                                 return new {
                                     Empty = !queue.TryPeek(out current),
                                     Value = current,
                                     Queue = queue,
                                 };

                             })).ToArray();

                // Wait for everything to complete
                Task.WaitAll(tasks);

                foreach (var task in tasks) {
                    if (!task.Result.Empty) {
                        // Keep track of the minimum element in the list
                        if (minElement == null || comparer.Compare(task.Result.Value, minElement) < 0) {
                            minElement = task.Result.Value;
                            minQueue = task.Result.Queue;
                        }
                    }
                    else {
                        // Remove the enumerator if it's empty
                        lazyQueues.Remove(task.Result.Queue);
                    }
                }

                if (lazyQueues.Any()) {
                    if (seen.Add(minElement)) {
                        yield return minElement;
                    }

                    // Clear the top of the enumerator we just peeked
                    minQueue.Dequeue();
                }

            } while (lazyQueues.Any());
        }

        private IList<IEnumerable<T>> GetSubQueries(Expression expression) {
            return _queryables.Select(query => GetSubQuery(query, expression)).ToList();
        }

        private IQueryable CreateQuery(Type elementType, Expression expression) {
            var queryType = typeof(AggregateQuery<>).MakeGenericType(elementType);
            var ctor = queryType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

            var subQueries = _subQueries;

            // Only update subqueries for ordering and where clauses
            if (QueryableUtility.IsQueryableMethod(expression, "Where") ||
                QueryableUtility.IsOrderingMethod(expression)) {
                subQueries = GetSubQueries(expression);
            }

            return (IQueryable)ctor.Invoke(new object[] { _queryables, _equalityComparer, subQueries, expression });
        }

        private static IEnumerable<T> GetSubQuery(IQueryable queryable, Expression expression) {
            expression = Rewrite(queryable, expression);

            IQueryable<T> newQuery = queryable.Provider.CreateQuery<T>(expression);

            // Create the query and only get up to the query cache size
            return new BufferedEnumerable<T>(newQuery, QueryCacheSize);
        }

        private static TResult Execute<TResult>(IQueryable queryable, Expression expression) {
            return queryable.Provider
                            .Execute<TResult>(Rewrite(queryable, expression));
        }

        private static object Execute(IQueryable queryable, Expression expression) {
            return queryable.Provider
                            .Execute(Rewrite(queryable, expression));
        }

        private static Expression RewriteForAggregation(IQueryable queryable, Expression expression) {
            // Remove filters, and ordering from the aggregate query
            return new ExpressionRewriter(queryable, new[] { "Where", 
                                                             "OrderBy", 
                                                             "OrderByDescending",
                                                             "ThenBy",
                                                             "ThenByDescending" }).Visit(expression);
        }

        private static Expression Rewrite(IQueryable queryable, Expression expression) {
            // Remove all take an skip andtake expression from individual linq providers
            return new ExpressionRewriter(queryable, new[] { "Skip", 
                                                             "Take" }).Visit(expression);
        }
    }
}

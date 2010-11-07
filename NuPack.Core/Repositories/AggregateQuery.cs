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
        private readonly IEnumerable<IEnumerable<T>> _subQueries;

        public AggregateQuery(IEnumerable<IQueryable<T>> queryables, IEqualityComparer<T> equalityComparer) {
            _queryables = queryables;
            _equalityComparer = equalityComparer;
            _expression = Expression.Constant(this);
            _subQueries = GetSubQueries(_expression);
        }

        private AggregateQuery(IEnumerable<IQueryable<T>> queryables,
                               IEqualityComparer<T> equalityComparer,
                               IEnumerable<IEnumerable<T>> subQueries,
                               Expression expression) {
            _queryables = queryables;
            _equalityComparer = equalityComparer;
            _expression = expression;
            _subQueries = subQueries;
        }

        public IEnumerator<T> GetEnumerator() {
            // Rewrite the expression for aggregation i.e. remove things that don't make sense to apply
            // after all initial expression has been applied.
            var aggregateQuery = GetAggregateEnumerable().AsQueryable();

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

            Type elementType = QueryableHelper.FindGenericType(typeof(IQueryable<>), expression.Type);

            if (elementType == null) {
                throw new ArgumentException(String.Empty, "expression");
            }

            return CreateQuery(elementType, expression);
        }

        public TResult Execute<TResult>(Expression expression) {
            var results = (from queryable in _queryables
                           select Execute<TResult>(queryable, expression)).AsQueryable();

            if (QueryableHelper.IsQueryableMethod(expression, "Count")) {
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
            var queue = new PriorityQueue<T>(new OrderingComparer<T>(Expression), _equalityComparer);
            var subQueryEnumerators = _subQueries.Select(query => query.GetEnumerator()).ToList();
            bool empty = false;

            while (!empty) {
                // Run tasks in parallel
                var tasks = (from e in subQueryEnumerators
                             select Task.Factory.StartNew(() => e.MoveNext() ? new { Empty = false, Value = e.Current }
                                                                             : new { Empty = true, Value = default(T) })
                             ).ToArray();

                // Wait for everything to complete
                Task.WaitAll(tasks);

                // Check each sub queries' enumerator and if there is more then add it to the queue
                foreach (var task in tasks) {
                    if (!task.Result.Empty) {
                        queue.Enqueue(task.Result.Value);
                    }
                }

                if (!queue.IsEmpty) {
                    // Return the item in the front of the queue
                    yield return queue.Dequeue();
                }
                else {
                    empty = tasks.All(t => t.Result.Empty);
                }

            }
        }

        private IEnumerable<IEnumerable<T>> GetSubQueries(Expression expression) {
            return _queryables.Select(query => GetSubQuery(query, expression));
        }

        private IQueryable CreateQuery(Type elementType, Expression expression) {
            var queryType = typeof(AggregateQuery<>).MakeGenericType(elementType);
            var ctor = queryType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

            var subQueries = _subQueries;

            // Only update subqueries for ordering and where clauses
            if (QueryableHelper.IsQueryableMethod(expression, "Where") ||
                QueryableHelper.IsOrderingMethod(expression)) {
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuPack {
    public class AggregateRepository : PackageRepositoryBase {
        private readonly IEnumerable<IPackageRepository> _repositories;
        public AggregateRepository(IEnumerable<IPackageRepository> repositories) {
            if (repositories == null) {
                throw new ArgumentNullException("repositories");
            }
            _repositories = repositories;
        }

        public override IQueryable<IPackage> GetPackages() {
            return new AggregateQuery<IPackage>(_repositories.Select(r => r.GetPackages()),
                                                PackageComparer.IdAndVersionComparer);
        }

        private class QueryableHelper {
            private static readonly MethodInfo[] _methods = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static);
            public static readonly MethodInfo CountMethod = _methods.First(m => m.Name == "Count");
            public static readonly MethodInfo TakeMethod = _methods.First(m => m.Name == "Take");


            public static MethodInfo GetQueryableMethod(Expression expression) {
                if (expression.NodeType == ExpressionType.Call) {
                    var call = (MethodCallExpression)expression;
                    if (call.Method.IsStatic && call.Method.DeclaringType == typeof(Queryable)) {
                        return call.Method.GetGenericMethodDefinition();
                    }
                }
                return null;
            }
        }

        private class AggregateQuery<T> : IQueryable<T>, IQueryProvider, IOrderedQueryable<T> {
            private readonly IEnumerable<IQueryable<T>> _queryables;
            private readonly IEqualityComparer<T> _distinctComparer;
            private readonly Expression _expression;

            public AggregateQuery(IEnumerable<IQueryable<T>> queryables, IEqualityComparer<T> distinctComparer) {
                _queryables = queryables;
                _expression = Expression.Constant(this);
                _distinctComparer = distinctComparer;
            }

            private AggregateQuery(IEnumerable<IQueryable<T>> queryables, IEqualityComparer<T> distinctComparer, Expression expression) {
                _queryables = queryables;
                _expression = expression;
                _distinctComparer = distinctComparer;
            }

            public IEnumerator<T> GetEnumerator() {
                // For each IQueryable<T> in our list, we apply each expression
                // and run them in parallel using PLINQ. After the results come back
                // we run our distinct comparer to remove duplicates
                IQueryable<T> aggregateQuery = _queryables.SelectMany(GetQuery)
                                                          .AsParallel()
                                                          .Distinct(_distinctComparer)
                                                          .AsQueryable();

                // Rewrite the expression for aggregation i.e. remove things that don't make sense to apply
                // after all initial expression has been applied.
                Expression aggregateExpression = RewriteForAggregattion(aggregateQuery, Expression);
                return aggregateQuery.Provider
                                     .CreateQuery<T>(aggregateExpression)
                                     .GetEnumerator();
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

                Type elementType = FindGenericType(typeof(IQueryable<>), expression.Type);

                if (elementType == null) {
                    throw new ArgumentException(String.Empty, "expression");
                }

                return CreateQuery(elementType, expression);
            }

            public TResult Execute<TResult>(Expression expression) {
                var results = (from queryable in _queryables
                               select Execute<TResult>(queryable, expression)).AsQueryable();

                if (QueryableHelper.GetQueryableMethod(expression) == QueryableHelper.CountMethod) {
                    // HACK: This is in correct since we aren't removing duplicates but count is mostly for paging
                    // so we don't care *that* much
                    return (TResult)(object)results.Cast<int>().Sum();
                }

                return Execute<TResult>(results, expression);
            }

            public object Execute(Expression expression) {
                return Execute<object>(expression);
            }

            private IQueryable CreateQuery(Type elementType, Expression expression) {
                var queryType = typeof(AggregateQuery<>).MakeGenericType(elementType);
                var ctor = queryType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
                return (IQueryable)ctor.Invoke(new object[] { _queryables, _distinctComparer, expression });
            }

            private IQueryable<T> GetQuery(IQueryable queryable) {
                return queryable.Provider.CreateQuery<T>(Rewrite(queryable, Expression));
            }

            private static TResult Execute<TResult>(IQueryable queryable, Expression expression) {
                return queryable.Provider.Execute<TResult>(Rewrite(queryable, expression));
            }

            private static object Execute(IQueryable queryable, Expression expression) {
                return queryable.Provider.Execute(Rewrite(queryable, expression));
            }

            private static Expression RewriteForAggregattion(IQueryable queryable, Expression expression) {
                return new AggregateRewriter(queryable).Visit(expression);
            }

            private static Expression Rewrite(IQueryable queryable, Expression expression) {
                return new ExpressionRewriter(queryable).Visit(expression);
            }

            private static Type FindGenericType(Type definition, Type type) {
                while ((type != null) && (type != typeof(object))) {
                    if (type.IsGenericType && (type.GetGenericTypeDefinition() == definition)) {
                        return type;
                    }
                    if (definition.IsInterface) {
                        foreach (Type interfaceType in type.GetInterfaces()) {
                            Type genericType = FindGenericType(definition, interfaceType);
                            if (genericType != null) {
                                return genericType;
                            }
                        }
                    }
                    type = type.BaseType;
                }
                return null;
            }

            private class AggregateRewriter : ExpressionRewriter {
                public AggregateRewriter(IQueryable aggregateQuery)
                    : base(aggregateQuery) {
                }

                protected override Expression VisitMethodCall(MethodCallExpression node) {
                    // We're removing expressions that we don't want to aggregate over. So far 
                    // that's everything except for take(n)
                    if (QueryableHelper.GetQueryableMethod(node) != QueryableHelper.TakeMethod) {
                        return Visit(node.Arguments[0]);
                    }

                    return base.VisitMethodCall(node);
                }
            }

            private class ExpressionRewriter : ExpressionVisitor {
                private readonly IQueryable _rootQuery;
                public ExpressionRewriter(IQueryable rootQuery) {
                    _rootQuery = rootQuery;
                }

                protected override Expression VisitConstant(ConstantExpression node) {
                    // Replace the query at the root of the expression
                    if (typeof(IQueryable).IsAssignableFrom(node.Type)) {
                        return _rootQuery.Expression;
                    }
                    return base.VisitConstant(node);
                }
            }
        }
    }
}

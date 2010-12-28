using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NuGet {
    /// <summary>
    /// There are some seurity issues around evaluating queries over EnumerableQuery with clousures.
    /// The compiler generates an internal type that can't be causes expression compilation to fail, when the 
    /// calling assembly is in the GAC and is SecurityTransparent. We wrap the underlying enumerable query and 
    /// then remove all compiler generated closures from the expression before compilation.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Type is an IQueryable and by convention should end with the term Query")]
    internal class SafeEnumerableQuery<T> : IQueryable<T>, IQueryProvider, IOrderedQueryable<T> {
        private readonly IQueryable _enumerableQuery;
        private readonly Expression _expression;

        public SafeEnumerableQuery(IEnumerable<T> enumerable) {
            if (enumerable == null) {
                throw new ArgumentNullException("enumerable");
            }
            _enumerableQuery = enumerable.AsQueryable();
            _expression = Expression.Constant(this);
        }

        private SafeEnumerableQuery(IQueryable enumerableQuery, Expression expression) {
            _enumerableQuery = enumerableQuery;
            _expression = expression;
        }

        public IEnumerator<T> GetEnumerator() {
            // Create the new query and return the enumerator
            return InnerProvider.CreateQuery<T>(InnerExpression).GetEnumerator();
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

        private IQueryProvider InnerProvider {
            get {
                return _enumerableQuery.Provider;
            }
        }

        internal Expression InnerExpression {
            get {
                // Rewrite all closures to use their values
                return GetInnerExpression(Expression);
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
            return InnerProvider.Execute<TResult>(GetInnerExpression(expression));
        }

        public object Execute(Expression expression) {
            return InnerProvider.Execute(GetInnerExpression(expression));
        }

        private Expression GetInnerExpression(Expression expression) {
            // First replace the this IQueryable with the enumerable query
            expression = QueryableUtility.ReplaceQueryableExpression(_enumerableQuery, expression);

            // Evaluate the closure variables
            return new ClosureEvaluator().Visit(expression);
        }

        private IQueryable CreateQuery(Type elementType, Expression expression) {
            var queryType = typeof(SafeEnumerableQuery<>).MakeGenericType(elementType);
            var ctor = queryType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

            return (IQueryable)ctor.Invoke(new object[] { _enumerableQuery, expression });
        }

        public override string ToString() {
            return _enumerableQuery.ToString();
        }
    }
}
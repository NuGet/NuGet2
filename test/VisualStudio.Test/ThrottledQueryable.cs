using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGet.VisualStudio.Test {

    /// <summary>
    /// This Queryable mimics the behavior of OData feed in that it sets a maximum limit of how 
    /// many items are returned by any query. We use this class to test the GetAll() extension method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ThrottledQueryable<T> : IQueryable<T>, IQueryProvider, IOrderedQueryable<T> {
        private readonly IQueryable _enumerableQuery;
        private readonly Expression _expression;

        public ThrottledQueryable(IEnumerable<T> enumerable) {
            if (enumerable == null) {
                throw new ArgumentNullException("enumerable");
            }
            _enumerableQuery = enumerable.AsQueryable();
            _expression = Expression.Constant(this);
        }

        private ThrottledQueryable(IQueryable enumerableQuery, Expression expression) {
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
                return GetInnerExpression(Expression);
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) {
            return (IQueryable<TElement>)CreateQuery(typeof(TElement), expression);
        }

        public IQueryable CreateQuery(Expression expression) {
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

            return new ModifyTakeExpressionVisitor(ElementType).Visit(expression);
        }

        private IQueryable CreateQuery(Type elementType, Expression expression) {
            var queryType = typeof(ThrottledQueryable<>).MakeGenericType(elementType);
            var ctor = queryType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

            return (IQueryable)ctor.Invoke(new object[] { _enumerableQuery, expression });
        }

        public override string ToString() {
            return _enumerableQuery.ToString();
        }
    }
}
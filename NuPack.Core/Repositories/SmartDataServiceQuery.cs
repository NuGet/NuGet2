using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Internal.Web.Utils;

namespace NuGet {
    /// <summary>
    /// Wrapper around OData's DataServiceRequest that switches to BatchQuery when the request URI becomes too long.
    /// </summary>
    /// <remarks>
    /// Batch queries are multiple queries tunneled via a single post. Post requests are never cached, therefore it is imperative that batch queries are used sparingly. 
    /// A formalized max-length is not specified, so we use 4k as per the analysis in http://www.boutell.com/newfaq/misc/urllength.html and only switch to batch queries when the url 
    /// exceeds this limit.
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Type is an IQueryable and by convention should end with the term Query")]
    public class SmartDataServiceQuery<T> : IQueryable<T>, IQueryProvider, IOrderedQueryable<T> {
        private readonly IDataServiceContext _context;
        private readonly IDataServiceQuery _query;

        public SmartDataServiceQuery(IDataServiceContext context, string entitySetName) {
            if (context == null) {
                throw new ArgumentNullException("context");
            }
            if (String.IsNullOrEmpty(entitySetName)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "entitySetName");
            }
            _context = context;
            _query = context.CreateQuery<T>(entitySetName);
            Expression = Expression.Constant(this);
        }

        private SmartDataServiceQuery(IDataServiceContext context, IDataServiceQuery query, Expression expression) {
            _context = context;
            _query = query;
            Expression = expression;
        }

        public IEnumerator<T> GetEnumerator() {
            DataServiceRequest request = _query.GetRequest(Expression);

            if (_query.RequiresBatch(Expression)) {
                return _context.ExecuteBatch<T>(request).GetEnumerator();
            }

            return _query.CreateQuery<T>(Expression).GetEnumerator();
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
            get;
            private set;
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
            return _query.Execute<TResult>(expression);
        }

        public object Execute(Expression expression) {
            return _query.Execute(expression);
        }

        private IQueryable CreateQuery(Type elementType, Expression expression) {
            var queryType = typeof(SmartDataServiceQuery<>).MakeGenericType(elementType);
            var ctor = queryType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

            return (IQueryable)ctor.Invoke(new object[] { _context, _query, expression });
        }

        public override string ToString() {
            return _query.CreateQuery<T>(Expression).ToString();
        }
    }
}

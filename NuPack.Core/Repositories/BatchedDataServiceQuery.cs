using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGet {
    /// <summary>
    /// Wrapper around OData's DataServiceRequest that switches to BatchQuery when the request URI becomes too long.
    /// </summary>
    /// <remarks>
    /// Batch queries are multiple queries tunneled via a single post. Post requests are never cached, therefore it is imperative that batch queries are used sparingly. 
    /// A formalized max-length is not specified, so we use 4k as per the analysis in http://www.boutell.com/newfaq/misc/urllength.html and only switch to batch queries when the url 
    /// exceeds this limit.
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification="Type is an IQueryable and by convention should end with the term Query")]
    public class BatchedDataServiceQuery<T> : IQueryable<T>, IQueryProvider, IOrderedQueryable<T> {
        private const int MaxUrlLength = 4000;
        private readonly DataServiceContext _context;
        private readonly IQueryable<T> _query;

        public BatchedDataServiceQuery(DataServiceContext context, string entitySetName)
            : this(context, context.CreateQuery<T>(entitySetName)) {
        }

        private BatchedDataServiceQuery(DataServiceContext context, IQueryable<T> query) {
            _context = context;
            _query = query;
        }

        public IEnumerator<T> GetEnumerator() {
            var request = (DataServiceQuery)_query;
            if (request.RequestUri.OriginalString.Length >= MaxUrlLength) {
                return _context.ExecuteBatch(request)
                               .Cast<QueryOperationResponse>()
                               .SelectMany(o => o.Cast<T>())
                               .GetEnumerator();
            }
            return _query.GetEnumerator();
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
                return _query.Expression;
            }
        }

        public IQueryProvider Provider {
            get {
                return this;
            }
        }

        internal IQueryProvider InnerProvider {
            get {
                return _query.Provider;
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
            return InnerProvider.Execute<TResult>(expression);
        }

        public object Execute(Expression expression) {
            return InnerProvider.Execute(expression);
        }

        private IQueryable CreateQuery(Type elementType, Expression expression) {
            var queryType = typeof(BatchedDataServiceQuery<>).MakeGenericType(elementType);
            var ctor = queryType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

            return (IQueryable)ctor.Invoke(new object[] { _context, InnerProvider.CreateQuery<T>(expression) });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace NuGet {
    public class DataServiceQueryWrapper<T> : IDataServiceQuery<T> {
        private const int MaxUrlLength = 4000;

        private readonly DataServiceQuery _query;
        private readonly IDataServiceContext _context;

        public DataServiceQueryWrapper(IDataServiceContext context, DataServiceQuery query) {
            if (query == null) {
                throw new ArgumentNullException("query");
            }
            _context = context;
            _query = query;
        }

        public bool RequiresBatch(Expression expression) {
            return GetRequest(expression).RequestUri.OriginalString.Length >= MaxUrlLength;
        }

        public DataServiceRequest GetRequest(Expression expression) {
            return (DataServiceRequest)_query.Provider.CreateQuery(GetInnerExpression(expression));
        }

        public TResult Execute<TResult>(Expression expression) {
            return _query.Provider.Execute<TResult>(GetInnerExpression(expression));
        }

        public object Execute(Expression expression) {
            return _query.Provider.Execute(GetInnerExpression(expression));
        }

        public IDataServiceQuery<TElement> CreateQuery<TElement>(Expression expression) {
            expression = GetInnerExpression(expression);

            var query = (DataServiceQuery)_query.Provider.CreateQuery<TElement>(expression);

            return new DataServiceQueryWrapper<TElement>(_context, query);
        }

        public IEnumerator<T> GetEnumerator() {
            return GetAll().GetEnumerator();
        }

        private IEnumerable<T> GetAll() {
            Type elementType = typeof(T);
            var results = _query.Execute();

            DataServiceQueryContinuation continuation = null;
            do {
                foreach (T item in results) {
                    // Get the concrete element type of the results returned
                    if (item != null && (elementType.IsInterface || elementType.IsAbstract)) {
                        elementType = item.GetType();
                    }
                    yield return item;
                }

                continuation = ((QueryOperationResponse)results).GetContinuation();

                if (continuation != null) {
                    // We need to execute the query with a concrete element type even though 
                    // we already have a <T>. This is because <T> might be an interface type
                    // which odata won't know how to create.
                    results = _context.Execute<T>(elementType, continuation);
                }

            } while (continuation != null);
        }

        private Expression GetInnerExpression(Expression expression) {
            return QueryableUtility.ReplaceQueryableExpression(_query, expression);
        }

        public override string ToString() {
            return _query.ToString();
        }
    }
}

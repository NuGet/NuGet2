using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Linq.Expressions;

namespace NuGet {
    public class DataServiceQueryWrapper<T> : IDataServiceQuery<T> {
        private const int MaxUrlLength = 4000;

        private readonly DataServiceQuery _query;

        public DataServiceQueryWrapper(DataServiceQuery query) {
            if (query == null) {
                throw new ArgumentNullException("query");
            }
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

            return new DataServiceQueryWrapper<TElement>(query);
        }

        public IEnumerator<T> GetEnumerator() {
            return ((IQueryable<T>)_query).GetEnumerator();
        }

        private Expression GetInnerExpression(Expression expression) {
            return QueryableUtility.ReplaceQueryableExpression(_query, expression);
        }
    }
}

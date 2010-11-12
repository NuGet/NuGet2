using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq.Expressions;

namespace NuGet {
    public class DataServiceQueryWrapper : IDataServiceQuery {
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

        public IEnumerator<T> GetEnumerator<T>(Expression expression) {
            return _query.Provider.CreateQuery<T>(GetInnerExpression(expression)).GetEnumerator();
        }

        public T Execute<T>(Expression expression) {
            return _query.Provider.Execute<T>(GetInnerExpression(expression));
        }

        public object Execute(Expression expression) {
            return _query.Provider.Execute(GetInnerExpression(expression));
        }

        private Expression GetInnerExpression(Expression expression) {
            return QueryableUtility.ReplaceQueryableExpression(_query, expression);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Globalization;
using System.Linq.Expressions;
using NuGet.Resources;

namespace NuGet {
    public class DataServiceQueryWrapper<T> : IDataServiceQuery<T> {
        private const int MaxUrlLength = 4000;

        private readonly DataServiceQuery _query;
        private readonly IDataServiceContext _context;
        private readonly Type _concreteType;

        public DataServiceQueryWrapper(IDataServiceContext context, DataServiceQuery query)
            : this(context, query, typeof(T)) {
        }

        public DataServiceQueryWrapper(IDataServiceContext context, DataServiceQuery query, Type concreteType) {
            if (context == null) {
                throw new ArgumentNullException("context");
            }

            if (query == null) {
                throw new ArgumentNullException("query");
            }

            _context = context;
            _query = query;
            _concreteType = concreteType;
        }

        public bool RequiresBatch(Expression expression) {
            return GetRequest(expression).RequestUri.OriginalString.Length >= MaxUrlLength;
        }

        public DataServiceRequest GetRequest(Expression expression) {
            return (DataServiceRequest)_query.Provider.CreateQuery(GetInnerExpression(expression));
        }

        public TResult Execute<TResult>(Expression expression) {
            return Execute(() => _query.Provider.Execute<TResult>(GetInnerExpression(expression)));
        }

        public object Execute(Expression expression) {
            return Execute(() => _query.Provider.Execute(GetInnerExpression(expression)));
        }

        public IDataServiceQuery<TElement> CreateQuery<TElement>(Expression expression) {
            expression = GetInnerExpression(expression);

            var query = (DataServiceQuery)_query.Provider.CreateQuery<TElement>(expression);

            return new DataServiceQueryWrapper<TElement>(_context, query, typeof(T));
        }

        public IEnumerator<T> GetEnumerator() {
            return GetAll().GetEnumerator();
        }

        private IEnumerable<T> GetAll() {
            IEnumerable results = Execute(_query.Execute);

            DataServiceQueryContinuation continuation = null;
            do {
                lock (_context) {
                    foreach (T item in results) {
                        yield return item;
                    }
                }

                continuation = ((QueryOperationResponse)results).GetContinuation();

                if (continuation != null) {
                    results = _context.Execute<T>(_concreteType, continuation);
                }

            } while (continuation != null);
        }

        private Expression GetInnerExpression(Expression expression) {
            return QueryableUtility.ReplaceQueryableExpression(_query, expression);
        }

        public override string ToString() {
            return _query.ToString();
        }

        private TResult Execute<TResult>(Func<TResult> action) {
            try {
                return action();
            }
            catch (Exception exception) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.InvalidFeed,
                    _context.BaseUri), exception);
            }
        }
    }
}

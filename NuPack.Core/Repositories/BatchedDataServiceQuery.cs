using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGet {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification="Type is an IQueryable and by convention should end with the term Query")]
    public class BatchedDataServiceQuery<T> : IQueryable<T>, IQueryProvider, IOrderedQueryable<T> {
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
            var request = (DataServiceRequest)_query;
            return _context.ExecuteBatch(request)
                           .Cast<QueryOperationResponse>()
                           .SelectMany(o => o.Cast<T>())
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

            Type elementType = FindGenericType(typeof(IQueryable<>), expression.Type);

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
    }
}

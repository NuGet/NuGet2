using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq.Expressions;

namespace NuGet {
    public interface IDataServiceQuery<T> : IDataServiceQuery {
        IEnumerator<T> GetEnumerator();
    }

    public interface IDataServiceQuery {
        bool RequiresBatch(Expression expression);
        DataServiceRequest GetRequest(Expression expression);
        IDataServiceQuery<TElement> CreateQuery<TElement>(Expression expression);
        TResult Execute<TResult>(Expression expression);
        object Execute(Expression expression);
    }
}

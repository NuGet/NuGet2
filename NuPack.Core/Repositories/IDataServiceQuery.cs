using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq.Expressions;

namespace NuGet {
    public interface IDataServiceQuery {
        bool RequiresBatch(Expression expression);
        DataServiceRequest GetRequest(Expression expression);
        IEnumerator<T> GetEnumerator<T>(Expression Expression);
        T Execute<T>(Expression expression);
        object Execute(Expression expression);
    }
}

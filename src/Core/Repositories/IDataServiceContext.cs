using System;
using System.Collections.Generic;
using System.Data.Services.Client;

namespace NuGet {
    public interface IDataServiceContext {
        Uri BaseUri { get; }
        bool IgnoreMissingProperties { get; set; }

        event EventHandler<SendingRequestEventArgs> SendingRequest;
        event EventHandler<ReadingWritingEntityEventArgs> ReadingEntity;

        IDataServiceQuery<T> CreateQuery<T>(string entitySetName);
        IEnumerable<T> ExecuteBatch<T>(DataServiceRequest request);

        Uri GetReadStreamUri(object entity);

        IEnumerable<T> Execute<T>(Type elementType, DataServiceQueryContinuation continuation);
    }
}

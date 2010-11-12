using System;
using System.Collections.Generic;
using System.Data.Services.Client;

namespace NuGet {
    public interface IDataServiceContext {
        bool IgnoreMissingProperties { get; set; }

        event EventHandler<SendingRequestEventArgs> SendingRequest;
        event EventHandler<ReadingWritingEntityEventArgs> ReadingEntity;

        IDataServiceQuery CreateQuery<T>(string entitySetName);
        IEnumerable<T> ExecuteBatch<T>(DataServiceRequest query);

        Uri GetReadStreamUri(object entity);
    }
}

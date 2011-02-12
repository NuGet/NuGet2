using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Reflection;

namespace NuGet {
    public class DataServiceContextWrapper : IDataServiceContext {
        private static readonly MethodInfo _executeMethodInfo = typeof(DataServiceContext).GetMethod("Execute", new[] { typeof(Uri) });
        private readonly DataServiceContext _context;

        public DataServiceContextWrapper(Uri serviceRoot) {
            if (serviceRoot == null) {
                throw new ArgumentNullException("serviceRoot");
            }
            _context = new DataServiceContext(serviceRoot);
            _context.MergeOption = MergeOption.OverwriteChanges;
        }

        public Uri BaseUri {
            get {
                return _context.BaseUri;
            }
        }

        public event EventHandler<SendingRequestEventArgs> SendingRequest {
            add {
                _context.SendingRequest += value;
            }
            remove {
                _context.SendingRequest -= value;
            }
        }

        public event EventHandler<ReadingWritingEntityEventArgs> ReadingEntity {
            add {
                _context.ReadingEntity += value;
            }
            remove {
                _context.ReadingEntity -= value;
            }
        }

        public bool IgnoreMissingProperties {
            get {
                return _context.IgnoreMissingProperties;
            }
            set {
                _context.IgnoreMissingProperties = value;
            }
        }

        public IDataServiceQuery<T> CreateQuery<T>(string entitySetName) {
            return new DataServiceQueryWrapper<T>(this, _context.CreateQuery<T>(entitySetName));
        }

        public IEnumerable<T> Execute<T>(Type elementType, DataServiceQueryContinuation continuation) {
            // Get the generic execute method method
            MethodInfo executeMethod = _executeMethodInfo.MakeGenericMethod(elementType);

            // Get the results from the continuation
            return (IEnumerable<T>)executeMethod.Invoke(_context, new object[] { continuation.NextLinkUri });
        }

        public IEnumerable<T> ExecuteBatch<T>(DataServiceRequest request) {
            return _context.ExecuteBatch(request)
                           .Cast<QueryOperationResponse>()
                           .SelectMany(o => o.Cast<T>());
        }


        public Uri GetReadStreamUri(object entity) {
            return _context.GetReadStreamUri(entity);
        }
    }
}

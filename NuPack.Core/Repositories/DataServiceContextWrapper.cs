using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;

namespace NuGet {
    public class DataServiceContextWrapper : IDataServiceContext {
        private readonly DataServiceContext _context;
        public DataServiceContextWrapper(Uri serviceRoot) {
            if (serviceRoot == null) {
                throw new ArgumentNullException("serviceRoot");
            }
            _context = new DataServiceContext(serviceRoot);
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

        public IDataServiceQuery CreateQuery<T>(string entitySetName) {
            return new DataServiceQueryWrapper(_context.CreateQuery<T>(entitySetName));
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

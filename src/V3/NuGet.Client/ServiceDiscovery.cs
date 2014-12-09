using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Diagnostics;

namespace NuGet.ServiceDiscovery
{
    public class ServiceDiscovery
    {
        TimeSpan _serviceIndexDocumentExpiration = TimeSpan.FromSeconds(1);

        Uri _serviceIndexUri;
        JObject _serviceIndexDocument;
        object _serviceIndexDocumentLock;
        IDictionary<string, IList<Uri>> _serviceEndpointsMap;
        System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

        DateTime _serviceIndexDocumentUpdateTime;
        bool _serviceIndexDocumentUpdating;

        private ServiceDiscovery(Uri serviceIndexUri, JObject serviceIndexDocument)
        {
            _serviceIndexUri = serviceIndexUri;
            _serviceIndexDocument = serviceIndexDocument;
            _serviceIndexDocumentUpdateTime = DateTime.UtcNow;
            _serviceIndexDocumentUpdating = false;
            _serviceIndexDocumentLock = new object();
        }

        public static async Task<ServiceDiscovery> Connect(Uri serviceIndexUri)
        {
            System.Net.Http.HttpClient hc = new System.Net.Http.HttpClient();
            string serviceIndexDocString = await hc.GetStringAsync(serviceIndexUri);
            JObject serviceIndexDoc = JObject.Parse(serviceIndexDocString);

            return new ServiceDiscovery(serviceIndexUri, serviceIndexDoc);
        }

        public IList<Uri> this[string type]
        {
            get
            {
                BeginUpdateServiceIndexDocument();

                JObject serviceIndexDocument;
                lock (_serviceIndexDocument)
                {
                    serviceIndexDocument = _serviceIndexDocument;
                }
                return serviceIndexDocument["resources"].Where(j => ((string)j["@type"]) == type).Select(o => o["@id"].ToObject<Uri>()).ToList();
            }
        }

        public async Task<string> GetStringAsync(string serviceType, string queryString)
        {
            BeginUpdateServiceIndexDocument();

            IList<Uri> uris = this[serviceType];

            List<Exception> exceptions = new List<Exception>();

            for (int i = 0; i < 2; ++i)
            {
                foreach (Uri uri in uris)
                {
                    string loc = uri.ToString() + queryString;
                    try
                    {
                        return await _httpClient.GetStringAsync(loc);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }

            throw new AggregateException(exceptions);
        }

        void BeginUpdateServiceIndexDocument()
        {
            lock (_serviceIndexDocumentLock)
            {
                if (_serviceIndexDocumentUpdating)
                    return;

                if (DateTime.UtcNow > _serviceIndexDocumentUpdateTime + _serviceIndexDocumentExpiration)
                {
                    _serviceIndexDocumentUpdating = true;
                    _httpClient.GetStringAsync(_serviceIndexUri).ContinueWith((t) =>
                    {
                        _serviceIndexDocumentUpdating = false;
                        JObject serviceIndexDocument = JObject.Parse(t.Result);
                        lock (_serviceIndexDocument)
                        {
                            _serviceIndexDocument = serviceIndexDocument;
                            _serviceIndexDocumentUpdateTime = DateTime.UtcNow;
                        }
                    });
                }
            }
        }
    }
}

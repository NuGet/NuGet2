using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Diagnostics;

namespace NuGet.Client
{
    // Let's not use NuGet's HttpClient for this.
    using HttpClient = System.Net.Http.HttpClient;

    public class ServiceDiscovery
    {
        class ServiceIndexDocument
        {
            public JObject Doc { get; private set; }
            public DateTime UpdateTime { get; private set; }

            public ServiceIndexDocument(JObject doc, DateTime updateTime)
            {
                Doc = doc;
                UpdateTime = updateTime;
            }
        }

        TimeSpan _serviceIndexDocumentExpiration = TimeSpan.FromMinutes(5);

        Uri _serviceIndexUri;
        ServiceIndexDocument _serviceIndexDocument;
        object _serviceIndexDocumentLock;
        HttpClient _httpClient = new HttpClient();

        bool _serviceIndexDocumentUpdating;

        private ServiceDiscovery(Uri serviceIndexUri, JObject serviceIndexDocument)
        {
            _serviceIndexUri = serviceIndexUri;
            _serviceIndexDocument = new ServiceIndexDocument(serviceIndexDocument, DateTime.UtcNow);
            _serviceIndexDocumentUpdating = false;
            _serviceIndexDocumentLock = new object();
        }

        public static async Task<ServiceDiscovery> Connect(Uri serviceIndexUri)
        {
            HttpClient hc = new HttpClient();
            string serviceIndexDocString = await hc.GetStringAsync(serviceIndexUri);
            JObject serviceIndexDoc = JObject.Parse(serviceIndexDocString);

            return new ServiceDiscovery(serviceIndexUri, serviceIndexDoc);
        }

        public IList<Uri> this[string type]
        {
            get
            {
                BeginUpdateServiceIndexDocument();
                return _serviceIndexDocument.Doc["resources"].Where(j => ((string)j["@type"]) == type).Select(o => o["@id"].ToObject<Uri>()).ToList();
            }
        }

        public async Task<string> GetStringAsync(string serviceType, string queryString)
        {
            IList<Uri> uris = this[serviceType];

            List<Exception> exceptions = new List<Exception>();

            // Try the whole list of endpoints twice each.
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
                        // Accumulate a list of exceptions from failed requests. They'll only
                        // be thrown if no request succeeds against any endpoint.
                        exceptions.Add(ex);
                    }
                }
            }

            throw new AggregateException(exceptions);
        }

        void BeginUpdateServiceIndexDocument()
        {
            // Get out quick if we don't have anything to do.
            if (_serviceIndexDocumentUpdating || DateTime.UtcNow <= _serviceIndexDocument.UpdateTime + _serviceIndexDocumentExpiration)
                return;

            // Lock to make sure that we can only attempt one update at a time.
            lock (_serviceIndexDocumentLock)
            {
                if (_serviceIndexDocumentUpdating)
                    return;

                _serviceIndexDocumentUpdating = true;
            }

            _httpClient.GetStringAsync(_serviceIndexUri).ContinueWith((t) =>
            {
                try
                {
                    JObject serviceIndexDocument = JObject.Parse(t.Result);
                    _serviceIndexDocument = new ServiceIndexDocument(serviceIndexDocument, DateTime.UtcNow);
                }
                finally
                {
                    _serviceIndexDocumentUpdating = false;
                }
            });
        }
    }
}

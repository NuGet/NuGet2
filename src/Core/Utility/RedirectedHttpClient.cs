using System;
using System.Globalization;
using System.Net;

namespace NuGet {
    /// <summary>
    /// This class should be used when ever you are using a link that is actually
    /// redirecting to the destination link that you want to use as the data source.
    /// A good example of that is a link that forwards like the current nuget link
    /// that is configured as a default location for nuget packages.
    /// </summary>
    public class RedirectedHttpClient : HttpClient {
        private IHttpClient _cachedRedirectClient = null;
        private Uri _originalUri = null;

        public RedirectedHttpClient(Uri uri)
            : base(uri) {
            _originalUri = uri;
        }

        public override WebRequest CreateRequest() {
            return CachedRedirectedClient.CreateRequest();
        }

        public override Uri Uri {
            get {
                return CachedRedirectedClient.Uri;
            }
        }

        private IHttpClient CachedRedirectedClient {
            get {
                // Cache an internal HttpClient object so that
                // we don't have to go through the forwarding link
                // every single time thus slowing down the connection to the
                // original source.
                if(_cachedRedirectClient == null) {
                    IHttpClient originalClient = new HttpClient(_originalUri);
                    WebRequest request = originalClient.CreateRequest();
                    using(WebResponse response = request.GetResponse()) {
                        if(null == response) {
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    "Unable to get a valid response for link: {0}",
                                    Uri.OriginalString));
                        }
                        _cachedRedirectClient = new HttpClient(response.ResponseUri);
                    }                    
                }
                return _cachedRedirectClient;
            }
        }
    }
}
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
        private Lazy<IHttpClient> _cachedClient = null;
        private Uri _originalUri = null;

        public RedirectedHttpClient(Uri uri)
            : base(uri) {
            _originalUri = uri;
            _cachedClient = new Lazy<IHttpClient>(EnsureClient);
        }

        public override WebRequest CreateRequest() {
            return _cachedClient.Value.CreateRequest();
        }

        public override Uri Uri {
            get {
                return _cachedClient.Value.Uri;
            }
        }

        private IHttpClient EnsureClient() {
            IHttpClient originalClient = new HttpClient(_originalUri);
            WebRequest request = originalClient.CreateRequest();
            using(WebResponse response = request.GetResponse()) {
                if(response == null) {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture,
                                      "Unable to get a valid response for link: {0}",
                                      Uri.OriginalString));
                }
                return new HttpClient(response.ResponseUri);
            }
        }
    }
}
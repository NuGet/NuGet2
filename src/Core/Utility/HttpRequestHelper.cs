using System;
using System.Net;

namespace NuGet {
    internal static class HttpRequestHelper {
        internal static HttpResponseData GetResponse(Uri uri, IWebProxy proxy = null, ICredentials credentials = null) {
            var key = GetCacheKey(uri, proxy, credentials);
            return MemoryCache.Instance.GetOrAdd<HttpResponseData>(key, () => GetResponseCore(uri, proxy, credentials), TimeSpan.FromSeconds(5));
        }

        internal static HttpResponseData GetCachedResponse(Uri uri, IWebProxy proxy = null, ICredentials credentials = null) {
            var key = GetCacheKey(uri, proxy, credentials);
            return MemoryCache.Instance.Get<HttpResponseData>(key);
        }

        private static HttpResponseData GetResponseCore(Uri uri, IWebProxy proxy = null, ICredentials credentials = null) {
            HttpWebResponse response = null;

            try {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);

                httpWebRequest.Credentials = credentials;
                httpWebRequest.Proxy = proxy;
                httpWebRequest.KeepAlive = true;
                httpWebRequest.ProtocolVersion = HttpVersion.Version10;

                response = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (WebException webException) {
                response = webException.Response as HttpWebResponse;
                if (response == null) {
                    throw;
                }
            }

            using (response) {
                return new HttpResponseData(response);
            }
        }

        private static Tuple<Uri, Uri, ICredentials, ICredentials> GetCacheKey(Uri uri, IWebProxy proxy, ICredentials credentials) {
            return Tuple.Create(uri, proxy.GetProxy(uri), proxy.Credentials, credentials);
        }
    }
}

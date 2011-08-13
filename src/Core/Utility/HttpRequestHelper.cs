using System;
using System.Net;

namespace NuGet {
    internal static class HttpRequestHelper {
        // List of supported authentication schemes
        private static readonly string[] _authenticationSchemes = new[] { "Basic", "NTLM" };

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

                httpWebRequest.Credentials = GetCredentials(uri, credentials);
                httpWebRequest.Proxy = proxy;
                httpWebRequest.KeepAlive = true;

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

        private static ICredentials GetCredentials(Uri uri, ICredentials credentials) {
            // No credentials then bail
            if (credentials == null) {
                return null;
            }

            // Do nothing with default credentials
            if (credentials == CredentialCache.DefaultCredentials || 
                credentials == CredentialCache.DefaultNetworkCredentials) {
                return credentials;
            }

            // If this isn't a NetworkCredential then leave it alove
            var networkCredentials = credentials as NetworkCredential;
            if (networkCredentials == null) {
                return credentials;
            }

            // Set this up for each authentication scheme we support
            // The reason we're using a credential cache is so that the HttpWebRequest will forward our
            // credentials if there happened to be any redirects in the chain of requests.
            var cache = new CredentialCache();
            foreach (var scheme in _authenticationSchemes) {
                cache.Add(uri, scheme, networkCredentials);    
            }
            return cache;
        }

        private static Tuple<Uri, Uri, ICredentials, ICredentials> GetCacheKey(Uri uri, IWebProxy proxy, ICredentials credentials) {
            return Tuple.Create(uri, proxy.GetProxy(uri), proxy.Credentials, credentials);
        }
    }
}

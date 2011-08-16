using System;
using System.Net;

namespace NuGet {
    internal static class RequestHelper {
        /// <summary>
        /// Keeps sending requests until a response code that doesn't require authentication happens or if
        /// the request requires authentication and the user has stopped trying to enter them (i.e they hit cancel when they are prompted).
        /// </summary>
        internal static WebResponse GetResponse(Func<WebRequest> createRequest,
                                                Action<WebRequest> prepareRequest,
                                                IProxyCache proxyCache,
                                                ICredentialCache credentialCache,
                                                ICredentialProvider credentialProvider) {
            HttpStatusCode? previousStatusCode = null;
            bool continueIfFailed = true;

            while (true) {
                // Create the request
                WebRequest request = createRequest();
                request.Proxy = proxyCache.GetProxy(request.RequestUri);

                if (previousStatusCode == null) {
                    // Try to use the cached credentials (if any, for the first request)
                    request.Credentials = credentialCache.GetCredentials(request.RequestUri);
                }
                else if (previousStatusCode == HttpStatusCode.ProxyAuthenticationRequired) {
                    request.Proxy.Credentials = credentialProvider.GetCredentials(request, useCredentialCache: false);

                    continueIfFailed = request.Proxy.Credentials != null;
                }
                else if (previousStatusCode == HttpStatusCode.Unauthorized) {
                    request.Credentials = credentialProvider.GetCredentials(request);

                    continueIfFailed = request.Credentials != null;
                }

                try {
                    // Prepare the request, we do something like write to the request stream
                    // which needs to happen last before the request goes out
                    prepareRequest(request);

                    WebResponse response = request.GetResponse();

                    // Cache the proxy and credentials
                    proxyCache.Add(request.Proxy);

                    credentialCache.Add(request.RequestUri, request.Credentials);
                    credentialCache.Add(response.ResponseUri, request.Credentials);

                    return response;
                }
                catch (WebException ex) {
                    IHttpWebResponse response = GetResponse(ex.Response);
                    if (response == null) {
                        // No response, someting went wrong so just rethrow
                        throw;
                    }

                    // If we were trying to authenticate the proxy or the request and succeeded, cache the result.
                    if (previousStatusCode == HttpStatusCode.ProxyAuthenticationRequired &&
                        response.StatusCode != HttpStatusCode.ProxyAuthenticationRequired) {
                        proxyCache.Add(request.Proxy);
                    }
                    else if (previousStatusCode == HttpStatusCode.Unauthorized &&
                             response.StatusCode != HttpStatusCode.Unauthorized) {

                        credentialCache.Add(request.RequestUri, request.Credentials);
                        credentialCache.Add(response.ResponseUri, request.Credentials);
                    }

                    if (!IsAuthenticationResponse(response) || !continueIfFailed) {
                        throw;
                    }

                    using (response) {
                        previousStatusCode = response.StatusCode;
                    }
                }
            }
        }

        private static IHttpWebResponse GetResponse(WebResponse response) {
            var httpWebResponse = response as IHttpWebResponse;
            if (httpWebResponse == null) {
                var webResponse = response as HttpWebResponse;
                if (webResponse == null) {
                    return null;
                }
                return new HttpWebResponseWrapper(webResponse);
            }

            return httpWebResponse;
        }

        private static bool IsAuthenticationResponse(IHttpWebResponse response) {
            return response.StatusCode == HttpStatusCode.Unauthorized ||
                   response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired;
        }

        private class HttpWebResponseWrapper : IHttpWebResponse {
            private readonly HttpWebResponse _response;
            public HttpWebResponseWrapper(HttpWebResponse response) {
                _response = response;
            }

            public HttpStatusCode StatusCode {
                get {
                    return _response.StatusCode;
                }
            }

            public Uri ResponseUri {
                get {
                    return _response.ResponseUri;
                }
            }

            public void Dispose() {
                if (_response != null) {
                    _response.Close();
                }
            }
        }
    }
}

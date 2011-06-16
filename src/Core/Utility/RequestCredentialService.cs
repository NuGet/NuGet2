using System;
using System.Collections.Concurrent;
using System.Net;

namespace NuGet {
    public class RequestCredentialService: CredentialProviderRegistry, IRequestCredentialService {
        private readonly ConcurrentDictionary<Uri, ICredentials> _credentialCache = new ConcurrentDictionary<Uri, ICredentials>();
        
        public ICredentials GetCredentials(Uri uri) {
            return GetCredentials(uri, null);
        }
        public ICredentials GetCredentials(Uri uri, IWebProxy proxy) {
            if (uri == null) {
                throw new ArgumentNullException("uri");
            }

            var isAuthenticationRequired = IsAuthenticationRequired(uri, proxy);
            return isAuthenticationRequired ? GetCredentialsInternal(uri, proxy) : null;
        }

        /// <summary>
        /// This method is responsible for going through each registered provider
        /// and ask for valid credentials until the first instance is found and
        /// then cache them for subsequent requests.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        private ICredentials GetCredentialsInternal(Uri uri, IWebProxy proxy) {
            ICredentials result = null;

            if (_credentialCache.TryGetValue(uri, out result)) {
                return result;
            }

            foreach (var provider in RegisteredProviders) {
                var credentials = ExecuteProvider(provider, uri, proxy);
                if (credentials != null) {
                    result = credentials;
                    break;
                }
            }

            _credentialCache.TryAdd(uri, result);
            return result;
        }

        /// <summary>
        /// This method is responsible for executing the given ICredentialProvider and validate
        /// if the returned credentials are valid before continuing.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="uri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        protected virtual ICredentials ExecuteProvider(ICredentialProvider provider, Uri uri, IWebProxy proxy) {
            var credentials = provider.GetCredentials(uri, proxy);
            if (AreCredentialsValid(credentials, uri, proxy)) {
                return credentials;
            }
            return null;
        }
        /// <summary>
        /// This method is responsible for checking to see if the provided url requires
        /// authentication.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        private bool IsAuthenticationRequired(Uri uri, IWebProxy proxy) {
            var request = WebRequest.Create(uri);
            return !AreCredentialsValid(request.Credentials, uri, proxy);
        }

        /// <summary>
        /// This method is responsible for checking if the given credentials are valid for the given Uri.
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="uri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        protected virtual bool AreCredentialsValid(ICredentials credentials, Uri uri, IWebProxy proxy) {
            bool result = true;
            var webRequest = WebRequest.Create(uri);
            WebResponse webResponse = null;

            webRequest.Credentials = credentials;
            // If the provided proxy is null then keep the original proxy on the request.
            webRequest.Proxy = proxy;
            try {
                webResponse = webRequest.GetResponse();
            }
            catch (WebException webException) {
                var httpResponse = webException.Response as HttpWebResponse;
                if (httpResponse == null || httpResponse.StatusCode == HttpStatusCode.Unauthorized) {
                    result = false;
                }
            }
            finally {
                if (webResponse != null) {
                    ((IDisposable)webResponse).Dispose();
                }
            }
            return result;
        }
    }
}

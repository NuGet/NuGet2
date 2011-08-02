using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace NuGet {
    public class RequestCredentialService : IRequestCredentialService {
        ///// <summary>
        ///// Local cache of registered proxy providers to use when locating a valid proxy
        ///// to use for the given Uri.
        ///// </summary>
        private readonly HashSet<ICredentialProvider> _providerCache = new HashSet<ICredentialProvider>();
        /// <summary>
        /// Local cache of credential objects that is used to prevent the subsequent look ups of credentials
        /// for the already discovered credentials based on the Uri.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, ICredentials> _credentialCache = new ConcurrentDictionary<Uri, ICredentials>();

        /// <summary>
        /// Returns a list of already registered ICredentialProvider instances that one can enumerate
        /// </summary>
        public ICollection<ICredentialProvider> RegisteredProviders {
            get {
                return _providerCache;
            }
        }

        /// <summary>
        /// Allows the consumer to provide a list of credential providers to use
        /// for locating of different ICredentials instances.
        /// </summary>
        public void RegisterProvider(ICredentialProvider provider) {
            if (provider == null) {
                throw new ArgumentNullException("provider");
            }
            _providerCache.Add(provider);
        }

        /// <summary>
        /// Unregisters the specified credential provider from the proxy finder.
        /// </summary>
        /// <param name="provider"></param>
        public void UnregisterProvider(ICredentialProvider provider) {
            if (provider == null) {
                throw new ArgumentNullException("provider");
            }
            _providerCache.Remove(provider);
        }
        /// <summary>
        /// Returns an ICredentials object instance that represents a valid credential
        /// object that can be used for request authentication.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
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
                var credentialResult = provider.GetCredentials(uri, proxy);
                if (credentialResult.State == CredentialState.Abort) {
                    // return so that we don't cache null if the user has cancelled the credentials prompt.
                    return null;
                }
                else {
                    if (AreCredentialsValid(credentialResult.Credentials, uri, proxy)) {
                        result = credentialResult.Credentials;
                        _credentialCache.TryAdd(uri, result);
                        break;
                    }
                    else {
                        result = null;
                    }
                }
            }

            return result;
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
            webRequest.Proxy = proxy;
            try {
                var httpRequest = webRequest as HttpWebRequest;
                if (httpRequest != null) {
                    httpRequest.KeepAlive = true;
                    httpRequest.ProtocolVersion = HttpVersion.Version10;
                }
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

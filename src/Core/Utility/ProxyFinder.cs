using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace NuGet {
    public class ProxyFinder : CredentialProviderRegistry, IProxyFinder {
        /// <summary>
        /// Local Cache of Proxy objects that will store the Proxy that was discovered during the session
        /// and will return the cached proxy object instead of trying to perform proxy detection logic
        /// for the same Uri again.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, IWebProxy> _proxyCache = new ConcurrentDictionary<Uri, IWebProxy>();
        ///// <summary>
        ///// Local cache of registered proxy providers to use when locating a valid proxy
        ///// to use for the given Uri.
        ///// </summary>
        //private readonly ISet<IProxyProvider> _providerCache = new HashSet<IProxyProvider>();
        /// <summary>
        /// Returns an instance of a IWebProxy interface that is to be used for creating requests to the given Uri
        /// </summary>
        /// <param name="uri">The Uri object that the proxy is to be used for.</param>
        /// <returns></returns>
        public virtual IWebProxy GetProxy(Uri uri) {
            if (uri == null) {
                throw new ArgumentNullException("uri");
            }

            bool isSystemProxySet = IsSystemProxySet(uri);
            return isSystemProxySet ? GetProxyInternal(uri) : null;
        }

        /// <summary>
        /// Internal method that handles the logic of going through the Proxy detection logic and returns the
        /// correct instance of the IWebProxy object.
        /// </summary>
        /// <param name="uri">The Uri object that the proxy is to be used for.</param>
        /// <returns></returns>
        private IWebProxy GetProxyInternal(Uri uri) {
            WebProxy systemProxy = GetSystemProxy(uri);

            IWebProxy cachedProxy;
            if (_proxyCache.TryGetValue(systemProxy.Address, out cachedProxy)) {
                return cachedProxy;
            }

            systemProxy.Credentials = GetProxyCredentials(uri);

            // TODO: If the proxy that is returned is null do we really want to cache this?
            // PRO: Subsequent requests for the given Uri should automatically return a null
            //      proxy instance without going through the proxy detection logic.
            // CON: If the user incorrectly types the password or an invalid proxy instance
            //      is cached then the user has to re-start the "Client" to be able to re-try
            //      connecting to a valid proxy.
            _proxyCache.TryAdd(systemProxy.Address, systemProxy);

            return systemProxy;
        }

        private ICredentials GetProxyCredentials(Uri uri) {
            ICredentials result = null;

            foreach (var provider in RegisteredProviders) {
                var credentials = ExecuteProvider(provider, uri);
                if (credentials != null) {
                    result = credentials;
                    break;
                }
            }
            return result;
        }

        protected virtual ICredentials ExecuteProvider(ICredentialProvider provider, Uri uri) {
            var credentials = provider.GetCredentials(uri);
            if (credentials != null) {
                var systemProxy = GetSystemProxy(uri);
                systemProxy.Credentials = credentials;
                return IsProxyValid(systemProxy, uri) ? credentials : null;
            }
            return null;
        }

        /// <summary>
        /// Returns the system proxy to be used to create a request for the given Uri.
        /// </summary>
        /// <param name="uri">The Uri object that the proxy is to be used for.</param>
        /// <returns></returns>
        private static WebProxy GetSystemProxy(Uri uri) {
            // WebRequest.DefaultWebProxy seems to be more capable in terms of getting the default
            // proxy settings instead of the WebRequest.GetSystemProxy()
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            string proxyUrl = proxy.GetProxy(uri).OriginalString;
            return new WebProxy(proxyUrl);
        }

        /// <summary>
        /// Tries to validate that the given proxy can navigate to the given Uri.
        /// This is done by creating a WebRequest and asking for a response. Based on the response
        /// a True or False indicates if the proxy is valid for the given Uri.
        /// </summary>
        /// <param name="proxy">IWebProxy object instance to validate against the Uri</param>
        /// <param name="uri">The Uri to test the IWebProxy against.</param>
        /// <returns></returns>
        private static bool IsProxyValid(IWebProxy proxy, Uri uri) {
            bool result = true;
            WebRequest request = WebRequest.Create(uri);
            WebResponse response = null;
            // if we get a null proxy from the caller then don't use it and just re-set the same proxy that we
            // already have because I am seeing a strange performance hit when a new instance of a proxy is set
            // and it can take a few seconds to be changed before the method call continues.
            request.Proxy = proxy ?? request.Proxy;
            try {
                // During testing we have observed that some proxy setups will return a 200/OK response
                // even though the subsequent calls are not going to be valid for the same proxy
                // and the set of credentials thus giving the user a 407 error.
                // However having to cache the proxy when this service first get's a call and using
                // a cached proxy instance seemed to resolve this issue.
                response = request.GetResponse();
            }
            catch (WebException webException) {
                HttpWebResponse webResponse = webException.Response as HttpWebResponse;
                if (webResponse == null || webResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired) {
                    result = false;
                }
            }
            finally {
                if (response != null) {
                    ((IDisposable)response).Dispose();
                }
            }
            return result;
        }

        /// <summary>
        /// Return true or false if connecting through a proxy server
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static bool IsSystemProxySet(Uri uri) {
            // The reason for not calling the GetSystemProxy is because the object
            // that will be returned is no longer going to be the proxy that is set by the settings
            // on the users machine only the Address is going to be the same.
            // Not sure why the .NET team did not want to expose all of the usefull settings like
            // ByPass list and other settings that we can't get because of it.
            // Anyway the reason why we need the DefaultWebProxy is to see if the uri that we are
            // getting the proxy for to should be bypassed or not. If it should be bypassed then
            // return that we don't need a proxy and we should try to connect directly.
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            if (proxy != null) {
                Uri proxyAddress = new Uri(proxy.GetProxy(uri).AbsoluteUri);
                if (String.Equals(proxyAddress.AbsoluteUri, uri.AbsoluteUri)) {
                    return false;
                }
                bool bypassUri = proxy.IsBypassed(uri);
                if (bypassUri) {
                    return false;
                }
                proxy = new WebProxy(proxyAddress);
            }

            return proxy != null;
        }

    }
}
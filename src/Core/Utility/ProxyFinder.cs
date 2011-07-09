using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace NuGet {
    public class ProxyFinder : IProxyFinder {
        /// <summary>
        /// Local cache of registered proxy providers to use when locating a valid proxy
        /// to use for the given Uri.
        /// </summary>
        private readonly HashSet<ICredentialProvider> _providerCache = new HashSet<ICredentialProvider>();
        /// <summary>
        /// Local Cache of Proxy objects that will store the Proxy that was discovered during the session
        /// and will return the cached proxy object instead of trying to perform proxy detection logic
        /// for the same Uri again.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, IWebProxy> _proxyCache = new ConcurrentDictionary<Uri, IWebProxy>();
        /// <summary>
        /// Capture the default System Proxy so that it can be re-used by the IProxyFinder
        /// because we can't rely on WebRequest.DefaultWebProxy since someone can modify the DefaultWebProxy
        /// property and we can't tell if it was modified and if we are still using System Proxy Settings or not.
        /// One limitation of this method is that it does not look at the config file to get the defined proxy
        /// settings.
        /// </summary>
        private static readonly IWebProxy _originalSystemProxy = WebRequest.GetSystemWebProxy();

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

            IWebProxy result;
            if (_proxyCache.TryGetValue(systemProxy.Address, out result)) {
                return result;
            }

            foreach (var provider in RegisteredProviders) {
                var credentialResult = provider.GetCredentials(uri, GetSystemProxy(uri));
                // The discovery process was aborted so stop the process and return null;
                if (credentialResult.State == CredentialState.Abort) {
                    return null;
                }
                // Some sort of credentials were returned so lets validate them.
                else {
                    systemProxy.Credentials = credentialResult.Credentials;
                    // If the proxy with the new credentials are valid then
                    // set the result to the valid proxy and break out of the discovery process.
                    if (IsProxyValid(systemProxy, uri)) {
                        result = systemProxy;
                        // Cache the valid proxy result so that we can use it next time without having to go through
                        // the discovery process.
                        _proxyCache.TryAdd(systemProxy.Address, result);
                        break;
                    }
                    // The credentials were not valid so continue the discovery process.
                    else {
                        result = null;
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// Returns the system proxy to be used to create a request for the given Uri.
        /// </summary>
        /// <param name="uri">The Uri object that the proxy is to be used for.</param>
        /// <returns></returns>
        private static WebProxy GetSystemProxy(Uri uri) {
            // WebRequest.DefaultWebProxy seems to be more capable in terms of getting the default
            // proxy settings instead of the WebRequest.GetSystemProxy()
            var proxyUri = _originalSystemProxy.GetProxy(uri);
            return new WebProxy(proxyUri);
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
            WebRequest webRequest = WebRequest.Create(uri);
            WebResponse response = null;
            // if we get a null proxy from the caller then don't use it and just re-set the same proxy that we
            // already have because I am seeing a strange performance hit when a new instance of a proxy is set
            // and it can take a few seconds to be changed before the method call continues.
            webRequest.Proxy = proxy ?? webRequest.Proxy;
            try {
                // During testing we have observed that some proxy setups will return a 200/OK response
                // even though the subsequent calls are not going to be valid for the same proxy
                // and the set of credentials thus giving the user a 407 error.
                // However having to cache the proxy when this service first get's a call and using
                // a cached proxy instance seemed to resolve this issue.
                var httpRequest = webRequest as HttpWebRequest;
                if (httpRequest != null) {
                    httpRequest.KeepAlive = true;
                    httpRequest.ProtocolVersion = HttpVersion.Version10;
                }
                response = webRequest.GetResponse();
            }
            catch (WebException webException) {
                HttpWebResponse webResponse = webException.Response as HttpWebResponse;
                if (webResponse != null && webResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired) {
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
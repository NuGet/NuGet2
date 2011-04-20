using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace NuGet {
    public class ProxyService : IProxyService {

        // This is kind of hackish because we are caching the proxy for the
        // entire App domain for the specific Uri's because we don't have the same
        // instances of the proxy service all of the time.
        // Once we have some sort of a ServiceLocator pattern implemented
        // then we can use a normal instance based approach and rely on
        // the same instance of the proxy service to maintain the list of cached proxies
        private static readonly Dictionary<Uri, WebProxy> _proxyCache = new Dictionary<Uri, WebProxy>();

        private readonly ICredentialProvider _credentialProvider;

        public ProxyService()
            : this(new DefaultCredentialProvider()) {
        }

        public ProxyService(ICredentialProvider credentialProvider) {
            if (null == credentialProvider) {
                throw new ArgumentNullException("credentialProvider");
            }
            _credentialProvider = credentialProvider;
        }

        public virtual IWebProxy GetProxy(Uri uri) {
            if (null == uri) {
                throw new ArgumentNullException("uri");
            }

            bool isSystemProxySet = IsSystemProxySet(uri);
            return isSystemProxySet ? GetProxyInternal(uri) : null;
        }

        private IWebProxy GetProxyInternal(Uri uri) {
            WebProxy result = null;
            WebProxy systemProxy = GetSystemProxy(uri);

            if (_proxyCache.ContainsKey(systemProxy.Address)) {
                return _proxyCache[systemProxy.Address];
            }

            // Try and see if we have credentials saved for the system proxy first so that we can
            // validate and see if we should use them.
            if (_credentialProvider.HasCredentials(systemProxy.Address)) {
                WebProxy savedCredentialsProxy = GetSystemProxy(uri);
                savedCredentialsProxy.Credentials = _credentialProvider.GetCredentials(systemProxy.Address).FirstOrDefault();
                if (IsProxyValid(savedCredentialsProxy, uri)) {
                    result = savedCredentialsProxy;
                }
            }
            // If we did not find any saved credentials then let's try to use Default Credentials which is
            // used for Integrated Authentication
            if (null == result) {
                WebProxy integratedAuthProxy = GetSystemProxy(uri);
                integratedAuthProxy.Credentials = _credentialProvider.DefaultCredentials;
                if (IsProxyValid(integratedAuthProxy, uri)) {
                    result = integratedAuthProxy;
                }
            }
            // If we did not succeed in getting a proxy by this time then let's try and prompt the user for
            // credentials and do that until we have succeeded.
            if (null == result) {
                WebProxy noCredentialsProxy = GetSystemProxy(uri);
                bool validCredentials = false;
                bool retryCredentials = false;
                ICredentials basicCredentials = null;
                while (!validCredentials) {
                    // Get credentials for the proxy address and not the target url
                    // because we'll end up prompting the user for a proxy for each different
                    // package due to the packages having different urls.
                    basicCredentials = _credentialProvider.PromptUserForCredentials(systemProxy.Address, retryCredentials);
                    // If the provider returned credentials that are null that means the user cancelled the prompt
                    // and we want to stop at this point and return nothing.
                    if (null == basicCredentials) {
                        result = null;
                        retryCredentials = false;
                        break;
                    }
                    noCredentialsProxy.Credentials = basicCredentials;
                    if (IsProxyValid(noCredentialsProxy, uri)) {
                        validCredentials = true;
                    }
                    else {
                        retryCredentials = true;
                        validCredentials = false;
                    }
                }
                result = noCredentialsProxy;
            }

            Debug.Assert(null != result, "Proxy should not be null here.");

            if (null != result) {
                _proxyCache.Add(systemProxy.Address, result);
            }

            return result;
        }

        private static WebProxy GetSystemProxy(Uri uri) {
            // WebRequest.DefaultWebProxy seems to be more capable in terms of getting the default
            // proxy settings instead of the WebRequest.GetSystemProxy()
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            string proxyUrl = proxy.GetProxy(uri).AbsoluteUri;
            return new WebProxy(proxyUrl);
        }

        private static bool IsProxyValid(IWebProxy proxy, Uri uri) {
            bool result = true;
            WebRequest request = CreateRequest(uri);
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
                if (null == webResponse || webResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired) {
                    result = false;
                }
            }
            finally {
                if (null != response) {
                    ((IDisposable)response).Dispose();
                }
            }
            return result;
        }

        private static WebRequest CreateRequest(Uri uri) {
            IHttpClient client = new HttpClient(uri);
            WebRequest request = client.CreateRequest();
            return request;
        }

        // Return true or false if connecting through a proxy server
        public static bool IsSystemProxySet(Uri uri) {
            // The reason for not calling the GetSystemProxy is because the object
            // that will be returned is no longer going to be the proxy that is set by the settings
            // on the users machine only the Address is going to be the same.
            // Not sure why the .NET team did not want to expose all of the usefull settings like
            // ByPass list and other settings that we can't get because of it.
            // Anyway the reason why we need the DefaultWebProxy is to see if the uri that we are
            // getting the proxy for to should be bypassed or not. If it should be bypassed then
            // return that we don't need a proxy and we should try to connect directly.
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            if (null != proxy) {
                Uri proxyAddress = new Uri(proxy.GetProxy(uri).AbsoluteUri);
                bool bypassUri = proxy.IsBypassed(uri);
                if (bypassUri) {
                    return false;
                }
                proxy = new WebProxy(proxyAddress);
            }

            return null != proxy;
        }
    }
}
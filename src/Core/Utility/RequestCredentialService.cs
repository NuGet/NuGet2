using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace NuGet {
    public class RequestCredentialService : IRequestCredentialService {
        private const int MaxRetries = 3;

        //// <summary>
        //// Local cache of registered proxy providers to use when locating a valid proxy
        //// to use for the given Uri.
        //// </summary>
        private readonly HashSet<ICredentialProvider> _providerCache = new HashSet<ICredentialProvider>();
        /// <summary>
        /// Local cache of credential objects that is used to prevent the subsequent look ups of credentials
        /// for the already discovered credentials based on the Uri.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, ICredentials> _credentialCache = new ConcurrentDictionary<Uri, ICredentials>();

        public RequestCredentialService() {
            RegisterProvider(new PrefixCredentialsProvider(_credentialCache));
        }

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
        public ICredentials GetCredentials(Uri uri, IWebProxy proxy) {
            if (uri == null) {
                throw new ArgumentNullException("uri");
            }

            return GetCredentialsInternal(uri, proxy);
        }

        /// <summary>
        /// This method is responsible for going through each registered provider
        /// and ask for valid credentials until the first instance is found and
        /// then cache them for subsequent requests.
        /// </summary>
        private ICredentials GetCredentialsInternal(Uri uri, IWebProxy proxy) {
            ICredentials credentials;

            if (_credentialCache.TryGetValue(uri, out credentials)) {
                return credentials;
            }

            foreach (var provider in RegisteredProviders) {                                
                int tries = provider.AllowRetry ? MaxRetries : 1;

                for (; tries > 0; tries--) {
                    CredentialResult credentialResult = provider.GetCredentials(uri, proxy);

                    if (credentialResult == null) {
                        // No credentials, move to the next provider
                        tries = 0;
                    }
                    else if (credentialResult.State == CredentialState.Abort) {
                        // Return so that we don't cache null if the user has cancelled the credentials prompt.
                        return null;
                    }
                    else if (AreCredentialsValid(credentialResult.Credentials, uri, proxy)) {
                        return credentialResult.Credentials;
                    }
                }
            }

            return credentials;
        }

        /// <summary>
        /// This method is responsible for checking if the given credentials are valid for the given Uri.
        /// </summary>
        protected virtual bool AreCredentialsValid(ICredentials credentials, Uri uri, IWebProxy proxy) {
            HttpResponseData responseData = HttpRequestHelper.GetResponse(uri, proxy, credentials);


            if (responseData.StatusCode == HttpStatusCode.Unauthorized) {
                return false;
            }

            // Cache the credentials for this uri and the response uri
            _credentialCache.TryAdd(uri, credentials);
            _credentialCache.TryAdd(responseData.ResponseUri, credentials);
            return true;
        }

        /// <summary>
        /// This provider tries to use cached credentials (if any) for some matching prefix.
        /// e.g. if the url is http://foo/bar and we have credentials for http://foo, try those first
        /// </summary>
        private class PrefixCredentialsProvider : ICredentialProvider {
            private readonly IDictionary<Uri, ICredentials> _credentialCache;

            public PrefixCredentialsProvider(IDictionary<Uri, ICredentials> credentialCache) {
                _credentialCache = credentialCache;
            }

            public bool AllowRetry {
                get {
                    return false;
                }
            }

            public CredentialResult GetCredentials(Uri uri, IWebProxy proxy) {
                ICredentials credentials = _credentialCache.Where(pair => GetSchemeAndServer(pair.Key).Equals(GetSchemeAndServer(uri), StringComparison.OrdinalIgnoreCase))
                                                           .Select(pair => pair.Value)
                                                           .FirstOrDefault();

                if (credentials == null) {
                    return null;
                }

                return CredentialResult.Create(CredentialState.HasCredentials, credentials);
            }

            private static string GetSchemeAndServer(Uri uri) {
                return uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);
            }
        }
    }
}

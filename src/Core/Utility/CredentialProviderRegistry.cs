using System;
using System.Collections.Generic;

namespace NuGet {

    /// <summary>
    /// This service is responsible for providing the consumer with the correct ICredentials
    /// object instances required to properly establish communication with repositories.
    /// </summary>
    public class CredentialProviderRegistry : ICredentialProviderRegistry {
        private readonly ISet<ICredentialProvider> _providerCache = new HashSet<ICredentialProvider>();

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
            if (!_providerCache.Contains(provider)) {
                return;
            }
            _providerCache.Remove(provider);
        }
    }
}
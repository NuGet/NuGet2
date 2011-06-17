using System;
using System.Collections.Generic;
using System.Net;

namespace NuGet {
    /// <summary>
    /// This interface represents the basic interface that one needs to implement to
    /// be able to provide support to the consumer for when a proxy object is required.
    /// </summary>
    public interface IProxyFinder {
        /// <summary>
        /// Returns a list of already registered ICredentialProvider instances that one can enumerate
        /// </summary>
        ICollection<ICredentialProvider> RegisteredProviders {
            get;
        }
        /// <summary>
        /// Allows the consumer to provide a list of credential providers to use
        /// for locating of different ICredentials instances.
        /// </summary>
        void RegisterProvider(ICredentialProvider provider);
        /// <summary>
        /// Unregisters the specified credential provider from the proxy finder.
        /// </summary>
        /// <param name="provider"></param>
        void UnregisterProvider(ICredentialProvider provider);
        /// <summary>
        /// Returns an IWebProxy object instance that represents a valid
        /// proxy object that should be used for communicating.
        /// </summary>
        /// <param name="uri">The given Uri to get a proxy for.</param>
        /// <returns></returns>
        IWebProxy GetProxy(Uri uri);
    }
}
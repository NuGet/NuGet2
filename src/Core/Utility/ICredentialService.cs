using System;
using System.Collections.Generic;
using System.Net;

namespace NuGet {
    public interface ICredentialService {
        /// <summary>
        /// Returns a list of already registered ICredentialProvider instances that one can enumerate
        /// </summary>
        ICollection<ICredentialProvider> RegisteredProviders {get;}
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
        /// Returns an ICredentials object instance that represents a valid
        /// credential object that should be used for communicating.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        ICredentials GetCredentials(Uri uri, IWebProxy proxy);
        ICredentials GetCredentials(Uri uri);
    }
}

using System;
using System.Net;

namespace NuGet {
    public interface IRequestCredentialService: ICredentialProviderRegistry {
        ///// <summary>
        /// Returns an ICredentials object instance that represents a valid credential
        /// object that can be used for request authentication.
        ///// </summary>
        ///// <param name="uri"></param>
        ///// <returns></returns>
        ICredentials GetCredentials(Uri uri);
        /// <summary>
        /// Returns an ICredentials object instance that represents a valid credential
        /// object that can be used for request authentication.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        ICredentials GetCredentials(Uri uri, IWebProxy proxy);
    }
}
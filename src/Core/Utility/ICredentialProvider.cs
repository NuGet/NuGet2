using System;
using System.Net;

namespace NuGet {
    /// <summary>
    /// This interface represents the basic interface that one needs to implement in order to
    /// support repository authentication. 
    /// </summary>
    public interface ICredentialProvider {

        ICredentials GetCredentials(Uri uri);
        /// <summary>
        /// Returns an ICredentials instance that the consumer would need in order
        /// to properly authenticate to the given Uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        ICredentials GetCredentials(Uri uri, IWebProxy proxy);
    }
}
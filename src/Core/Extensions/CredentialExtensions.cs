using System;
using System.Net;

namespace NuGet {
    public static class CredentialExtensions {
        public static CredentialResult GetCredentials(this ICredentialProvider provider, Uri uri) {
            return provider.GetCredentials(uri, null);
        }
        ///// <summary>
        /// Returns an ICredentials object instance that represents a valid credential
        /// object that can be used for request authentication.
        ///// </summary>
        ///// <param name="uri"></param>
        ///// <returns></returns>
        public static ICredentials GetCredentials(this IRequestCredentialService requestCredentialService, Uri uri) {
            return requestCredentialService.GetCredentials(uri, null);
        }
    }
}

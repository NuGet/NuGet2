using System;
using System.Net;

namespace NuGet {
    /// <summary>
    /// Provides sytems integrated credentials for NTLM/Integrated authentication
    /// type of proxy.
    /// </summary>
    public class IntegratedCredentialProvider : ICredentialProvider {
        public CredentialState GetCredentials(Uri uri, out ICredentials credentials) {
            return GetCredentials(uri, null, out credentials);
        }
        public CredentialState GetCredentials(Uri uri, IWebProxy proxy, out ICredentials credentials) {
            credentials = CredentialCache.DefaultCredentials;
            return CredentialState.HasCredentials;
        }
    }
}
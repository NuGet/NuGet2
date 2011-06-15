using System;
using System.Net;

namespace NuGet {
    /// <summary>
    /// Provides sytems integrated credentials for NTLM/Integrated authentication
    /// type of proxy.
    /// </summary>
    public class IntegratedCredentialProvider : ICredentialProvider {
        public ICredentials GetCredentials(Uri uri) {
            return GetCredentials(uri, null);
        }
        public ICredentials GetCredentials(Uri uri, IWebProxy proxy) {
            return CredentialCache.DefaultCredentials;
        }
    }
}
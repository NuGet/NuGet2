using System;
using System.Net;

namespace NuGet {
    /// <summary>
    /// Provides sytems integrated credentials for NTLM/Integrated authentication
    /// type of proxy.
    /// </summary>
    public class IntegratedCredentialProvider : ICredentialProvider {
        public CredentialResult GetCredentials(Uri uri, IWebProxy proxy) {
            return CredentialResult.Create(CredentialState.HasCredentials, CredentialCache.DefaultCredentials);
        }

        public bool AllowRetry {
            get { return false; }
        }
    }
}
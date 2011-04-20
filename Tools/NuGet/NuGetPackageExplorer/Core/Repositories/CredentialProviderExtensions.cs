using System;
using System.Linq;
using System.Net;

namespace NuGet {
    public static class CredentialProviderExtensions {
        public static bool HasCredentials(this ICredentialProvider provider, Uri uri) {
            ICredentials[] credentials = provider.GetCredentials(uri);
            return credentials != null && credentials.Count() > 0;
        }
    }
}
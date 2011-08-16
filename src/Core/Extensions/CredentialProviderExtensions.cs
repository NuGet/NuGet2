using System;
using System.Net;

namespace NuGet {
    internal static class CredentialProviderExtensions {
        private static readonly string[] _authenticationSchemes = new[] { "Basic", "NTLM" };

        internal static ICredentials GetCredentials(this ICredentialProvider provider, WebRequest request, bool useCredentialCache = true) {
            ICredentials credentials = provider.GetCredentials(request.RequestUri, request.Proxy);

            if (credentials == null) {
                return null;
            }

            if (!useCredentialCache) {
                return credentials;
            }

            return WrapCredentials(request.RequestUri, credentials);
        }

        private static ICredentials WrapCredentials(Uri uri, ICredentials credentials) {
            // No credentials then bail
            if (credentials == null) {
                return null;
            }

            // Do nothing with default credentials
            if (credentials == CredentialCache.DefaultCredentials ||
                credentials == CredentialCache.DefaultNetworkCredentials) {
                return credentials;
            }

            // If this isn't a NetworkCredential then leave it alove
            var networkCredentials = credentials as NetworkCredential;
            if (networkCredentials == null) {
                return credentials;
            }

            // Set this up for each authentication scheme we support
            // The reason we're using a credential cache is so that the HttpWebRequest will forward our
            // credentials if there happened to be any redirects in the chain of requests.
            var cache = new CredentialCache();
            foreach (var scheme in _authenticationSchemes) {
                cache.Add(uri, scheme, networkCredentials);
            }
            return cache;
        }
    }
}

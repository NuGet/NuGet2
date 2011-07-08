using System;
using System.Net;

namespace NuGet.VisualStudio {
    public class VSProxyCredentialProvider: VisualStudioCredentialProvider {
        protected override void InitializeCredentialProxy(Uri uri, IWebProxy originalProxy) {
            WebRequest.DefaultWebProxy = new WebProxy(originalProxy.GetProxy(uri));
        }

        protected override bool AreCredentialsValid(Uri uri, ICredentials credentials, IWebProxy proxy) {
            var webRequest = WebRequest.Create(uri);
            proxy.Credentials = credentials;
            webRequest.Proxy = proxy;
            try {
                webRequest.GetResponse();
            }
            catch (WebException webException) {
                // Only handle what we know so check to see if it is a Proxy Authentication error
                // and return false otherwise it might be a server error
                // and we don't want to be responsible for handling those errors here.
                var webResponse = webException.Response as HttpWebResponse;
                if (webResponse != null && webResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired) {
                    return false;
                }
            }
            return true;
        }
    }
}

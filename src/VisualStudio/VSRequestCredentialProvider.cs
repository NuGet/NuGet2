using System;
using System.Net;

namespace NuGet.VisualStudio {
    public class VSRequestCredentialProvider : VisualStudioCredentialProvider {
        protected override void InitializeCredentialProxy(Uri uri, IWebProxy originalProxy) {
            WebRequest.DefaultWebProxy = new WebProxy(uri);
        }

        protected override bool AreCredentialsValid(Uri uri, ICredentials credentials, IWebProxy proxy) {
            var webRequest = WebRequest.Create(uri);
            webRequest.Credentials = credentials;
            webRequest.Proxy = proxy ?? webRequest.Proxy;
            try {
                var httpRequest = webRequest as HttpWebRequest;
                if (httpRequest != null) {
                    httpRequest.KeepAlive = true;
                    httpRequest.ProtocolVersion = HttpVersion.Version10;
                }
                webRequest.GetResponse();
            }
            catch (WebException webException) {
                // Only handle what we know so check to see if it is an Unauthorized error
                // and return false otherwise it might be a server error
                // and we don't want to be responsible for handling those errors here.
                var webResponse = webException.Response as HttpWebResponse;
                if (webException.Status == WebExceptionStatus.ReceiveFailure
                    || (webResponse != null && webResponse.StatusCode == HttpStatusCode.Unauthorized)) {
                    return false;
                }
            }
            return true;
        }
    }
}

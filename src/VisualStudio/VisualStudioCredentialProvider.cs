using System;
using System.Net;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio {
    public class VisualStudioCredentialProvider : BaseProxyProvider {
        private IVsWebProxy _webProxyService;

        public VisualStudioCredentialProvider()
            : this(ServiceLocator.GetGlobalService<SVsWebProxy, IVsWebProxy>()) {
        }

        public VisualStudioCredentialProvider(IVsWebProxy webProxyService) {
            if (webProxyService == null) {
                throw new ArgumentNullException("webProxyService");
            }
            _webProxyService = webProxyService;
        }

        public override IWebProxy GetProxy(Uri uri) {
            bool forcePrompt = false;
            while (true) {
                var hasCachedCredentials = HasSavedCredentials(uri);
                if (forcePrompt || !hasCachedCredentials) {
                    GetCredentials(uri, forcePrompt: true);
                }
                if (IsValidProxy(uri, WebRequest.DefaultWebProxy)) {
                    break;
                }
                else {
                    forcePrompt = true;
                }
            }
            return WebRequest.DefaultWebProxy;
        }

        private bool IsValidProxy(Uri uri, IWebProxy proxy) {
            var request = WebRequest.Create(uri);
            request.Proxy = proxy;
            try {
                request.GetResponse();
            }
            catch (WebException webException) {
                // Only handle what we know so check to see if it is a proxy error
                // and return false otherwise it might be a server error
                // and we don't want to be responsible for handling those errors here.
                var webResponse = webException.Response as HttpWebResponse;
                if (webResponse != null && webResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// This method checks to see if the user has already saved credentials for the given Url.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private bool HasSavedCredentials(Uri uri) {
            return GetCredentials(uri, false) != null;
        }

        /// <summary>
        /// This method is responsible for retrieving either cached credentials
        /// or forcing a prompt if we need the user to give us new credentials.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="forcePrompt"></param>
        /// <returns></returns>
        private ICredentials GetCredentials(Uri uri, bool forcePrompt) {
            __VsWebProxyState oldState;
            if (forcePrompt) {
                oldState = __VsWebProxyState.VsWebProxyState_PromptForCredentials;
            }
            else {
                oldState = __VsWebProxyState.VsWebProxyState_DefaultCredentials;
            }
            var newState = (uint)__VsWebProxyState.VsWebProxyState_NoCredentials;
            int result = 0;

            ThreadHelper.Generic.Invoke(() => {
                result = _webProxyService.PrepareWebProxy(uri.OriginalString,
                                                      (uint)oldState,
                                                      out newState,
                                                      Convert.ToInt32(forcePrompt));
            });
            // If result is anything but 0 that most likely means that there was an error
            // so we will null out the DefaultWebProxy.Credentials so that we don't get
            // invalid credentials stored for subsequent requests.
            if (result != 0 || newState == (uint)__VsWebProxyState.VsWebProxyState_Abort) {
                // Clear out the current credentials because the user might have clicked cancel
                // and we don't want to use the currently set credentials if they are wrong.
                WebRequest.DefaultWebProxy.Credentials = null;
                return null;
            }
            return WebRequest.DefaultWebProxy.Credentials;
        }
    }
}

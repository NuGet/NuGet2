using System;
using System.Net;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio {
    public class VisualStudioCredentialProvider: ICredentialProvider {
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

        public ICredentials GetCredentials(Uri uri) {
            return GetCredentials(uri, null);
        }

        /// <summary>
        /// Returns an ICredentials instance that the consumer would need in order
        /// to properly authenticate to the given Uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public ICredentials GetCredentials(Uri uri, IWebProxy proxy) {
            bool forcePrompt = false;
            ICredentials credentials = null;
            while (true) {
                var hasCachedCredentials = HasSavedCredentials(uri);
                if (forcePrompt || !hasCachedCredentials) {
                    var proxyState = GetCredentials(uri, true, out credentials);
                    if (proxyState == __VsWebProxyState.VsWebProxyState_Abort) {
                        break;
                    }
                }
                if (AreCredentialsValid(uri, credentials, proxy)) {
                    break;
                }
                forcePrompt = true;
            }
            return credentials;
        }

        private bool AreCredentialsValid(Uri uri, ICredentials credentials, IWebProxy proxy) {
            var webRequest = WebRequest.Create(uri);
            webRequest.Credentials = credentials;
            webRequest.Proxy = proxy ?? webRequest.Proxy;
            try {
                webRequest.GetResponse();
            }
            catch (WebException webException) {
                // Only handle what we know so check to see if it is an Unauthorized error
                // and return false otherwise it might be a server error
                // and we don't want to be responsible for handling those errors here.
                var webResponse = webException.Response as HttpWebResponse;
                if (webResponse != null && webResponse.StatusCode == HttpStatusCode.Unauthorized) {
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
            ICredentials credentials = null;
            GetCredentials(uri, false, out credentials);
            return credentials != null;
        }

        /// <summary>
        /// This method is responsible for retrieving either cached credentials
        /// or forcing a prompt if we need the user to give us new credentials.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="forcePrompt"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        private __VsWebProxyState GetCredentials(Uri uri, bool forcePrompt, out ICredentials credentials) {
            __VsWebProxyState oldState;
            oldState = forcePrompt 
                ? __VsWebProxyState.VsWebProxyState_PromptForCredentials 
                : __VsWebProxyState.VsWebProxyState_DefaultCredentials;
            var newState = (uint)__VsWebProxyState.VsWebProxyState_NoCredentials;
            int result = 0;
            var defaultCredentials = WebRequest.DefaultWebProxy.Credentials;
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
                WebRequest.DefaultWebProxy.Credentials = defaultCredentials;
                credentials = null;
                return (__VsWebProxyState)newState;
            }
            // Get the new credentials from the proxy instance
            credentials = WebRequest.DefaultWebProxy.Credentials;
            // reset the proxy credentials to what they were before this process started.
            WebRequest.DefaultWebProxy.Credentials = defaultCredentials;
            // return new credentials.
            return (__VsWebProxyState)newState;
        }
    }
}

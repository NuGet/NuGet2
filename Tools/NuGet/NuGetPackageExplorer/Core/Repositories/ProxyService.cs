using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NuGet.Utility;
using System.Net;

namespace NuGet.Repositories
{
    public class ProxyService : IProxyService
    {
        ICredentialProvider _credentialProvider;

        public ProxyService()
            : this(new DefaultCredentialProvider())
        {
        }
        public ProxyService(ICredentialProvider credentialProvider)
        {
            if (null == credentialProvider)
            {
                throw new ArgumentNullException("credentialProvider");
            }
            _credentialProvider = credentialProvider;
        }
        public virtual IWebProxy GetProxy(Uri uri)
        {
            if (null == uri)
            {
                throw new ArgumentNullException("uri");
            }

            return IsSystemProxySet() ? GetProxyInternal(uri) : null;
        }

        private IWebProxy GetProxyInternal(Uri uri)
        {
            IWebProxy result = null;
            WebProxy systemProxy = GetSystemProxy(uri);
            // Try and see if we have credentials saved for the system proxy first so that we can
            // validate and see if we should use them.
            if (_credentialProvider.HasCredentials(systemProxy.Address))
            {
                WebProxy savedCredentialsProxy = GetSystemProxy(uri);
                savedCredentialsProxy.Credentials = _credentialProvider.GetCredentials(systemProxy.Address).FirstOrDefault();
                if (IsProxyValid(savedCredentialsProxy, uri))
                {
                    result = savedCredentialsProxy;
                }
            }
            // If we did not find any saved credentials then let's try to use Default Credentials which is
            // used for Integrated Authentication
            if (null == result)
            {
                IWebProxy integratedAuthProxy = GetSystemProxy(uri);
                integratedAuthProxy.Credentials = _credentialProvider.DefaultCredentials;
                if (IsProxyValid(integratedAuthProxy, uri))
                {
                    result = integratedAuthProxy;
                }
            }
            // If we did not succeed in getting a proxy by this time then let's try and prompt the user for
            // credentials and do that until we have succeeded.
            if (null == result)
            {
                WebProxy noCredentialsProxy = GetSystemProxy(uri);
                bool validCredentials = false;
                bool retryCredentials = false;
                ICredentials basicCredentials = null;
                while (!validCredentials)
                {
                    // Get credentials for the proxy address and not the target url
                    // because we'll end up prompting the user for a proxy for each different
                    // package due to the packages having different urls.
                    basicCredentials = _credentialProvider.PromptUserForCredentials(systemProxy.Address, retryCredentials);
                    // If the provider returned credentials that are null that means the user cancelled the prompt
                    // and we want to stop at this point and return nothing.
                    if (null == basicCredentials)
                    {
                        result = null;
                        retryCredentials = false;
                        break;
                    }
                    noCredentialsProxy.Credentials = basicCredentials;
                    if (IsProxyValid(noCredentialsProxy, uri))
                    {
                        validCredentials = true;
                    }
                    else
                    {
                        retryCredentials = true;
                        validCredentials = false;
                    }
                }
                result = noCredentialsProxy;
            }
            return result;
        }

        private WebProxy GetSystemProxy(Uri uri)
        {
            // Using WebRequest.GetSystemWebProxy() is the best way to get the default system configured
            // proxy settings which are retrieved from IE by default as per
            // http://msdn.microsoft.com/en-us/library/system.net.webrequest.getsystemwebproxy.aspx
            // The documentation states that this method also performs logic to automatically detect proxy settings,
            // use an automatic configuration script, and manual proxy server settings, and advanced manual proxy server settings.
            IWebProxy proxy = WebRequest.GetSystemWebProxy();
            string proxyUrl = proxy.GetProxy(uri).AbsoluteUri;
            WebProxy systemProxy = new WebProxy(proxyUrl);
            return systemProxy;
        }
        private bool IsProxyValid(IWebProxy proxy, Uri uri)
        {
            bool result = true;
            WebRequest request = CreateRequest(uri);
            WebResponse response = null;
            // if we get a null proxy from the caller then don't use it and just re-set the same proxy that we
            // already have because I am seeing a strange performance hit when a new instance of a proxy is set
            // and it can take a few seconds to be changed before the method call continues.
            request.Proxy = proxy ?? request.Proxy;
            try
            {
                response = request.GetResponse();
            }
            catch (WebException webException)
            {
                HttpWebResponse webResponse = webException.Response as HttpWebResponse;
                if (null == webResponse || webResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                {
                    result = false;
                }
            }
            finally
            {
                if (null != response)
                {
                    ((IDisposable)response).Dispose();
                }
            }
            return result;
        }
        private WebRequest CreateRequest(Uri uri)
        {
            IHttpClient client = new HttpClient(uri);
            WebRequest request = client.CreateRequest();
            return request;
        }


        [DllImport("wininet.dll", CharSet = CharSet.Auto)]
        private extern static bool InternetGetConnectedState(ref InternetConnectionState_e lpdwFlags, int dwReserved);


        [Flags]
        enum InternetConnectionState_e : int
        {
            INTERNET_CONNECTION_MODEM = 0x1,
            INTERNET_CONNECTION_LAN = 0x2,
            INTERNET_CONNECTION_PROXY = 0x4,
            INTERNET_RAS_INSTALLED = 0x10,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_CONFIGURED = 0x40
        }

        // Return true or false if connecting through a proxy server
        public bool IsSystemProxySet()
        {
            InternetConnectionState_e flags = 0;
            InternetGetConnectedState(ref flags, 0);
            bool hasProxy = false;

            hasProxy = (flags & InternetConnectionState_e.INTERNET_CONNECTION_PROXY) != 0;

            return hasProxy;
        }

    }
}

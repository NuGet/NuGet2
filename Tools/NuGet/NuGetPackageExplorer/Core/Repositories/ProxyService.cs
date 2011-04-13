using System;
using System.Collections.Generic;
using System.Linq;
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

            string url = uri.OriginalString;
            ProxyType proxyType = GetProxyType(url);
            WebProxy proxy = null;
            switch (proxyType)
            {
                case ProxyType.None:
                    proxy = null;
                    break;
                case ProxyType.IntegratedAuth:
                case ProxyType.BasicAuth:
                    proxy = GetSystemProxy(url);
                    bool validCredentials = false;
                    ICredentials basicCredentials = null;
                    while (!validCredentials)
                    {
                        basicCredentials = _credentialProvider.GetCredentials(proxyType, uri,true);
                        if(AreCredentialsValid(basicCredentials,uri.OriginalString))
                        {
                            validCredentials = true;
                        }
                        else
                        {
                            validCredentials = false;
                        }
                    }
                    proxy.Credentials = basicCredentials;
                    break;
            }
            return proxy;
        }

        private ProxyType GetProxyType(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            string[] proxyTypes = Enum.GetNames(typeof(ProxyType));

            for (int i = 0; i < proxyTypes.Length; i++)
            {
                ProxyType type = (ProxyType)Enum.Parse(typeof(ProxyType), proxyTypes[i]);
                switch (type)
                {
                    case ProxyType.None:
                        IWebProxy emptyWebproxy = WebRequest.GetSystemWebProxy();
                        if (IsProxyValid(emptyWebproxy, url))
                        {
                            return type;
                        }
                        break;
                    case ProxyType.IntegratedAuth:
                        WebProxy integratedAuthProxy = GetSystemProxy(url);
                        integratedAuthProxy.UseDefaultCredentials = true;
                        // Commenting out the Credentials setter based on the Remarks that can be found:
                        // http://msdn.microsoft.com/en-us/library/system.net.webrequest.usedefaultcredentials.aspx
                        // It is basically saying that it is best to set the UseDefaultCredentials for client applications
                        // and only use the Credentials property for middle tier applications such as ASP.NET applications.
                        //ntlmProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                        if (IsProxyValid(integratedAuthProxy, url))
                        {
                            return type;
                        }
                        break;
                    case ProxyType.BasicAuth:
                        // this is our last resort so we will simply return
                        // the ProxyType.Basic so that the user will be prompted
                        return type;
                }
            }
            return ProxyType.BasicAuth;
        }
        private WebProxy GetSystemProxy(string url)
        {
            // Using WebRequest.GetSystemWebProxy() is the best way to get the default system configured
            // proxy settings which are retrieved from IE by default as per
            // http://msdn.microsoft.com/en-us/library/system.net.webrequest.getsystemwebproxy.aspx
            // The documentation states that this method also performs logic to automatically detect proxy settings,
            // use an automatic configuration script, and manual proxy server settings, and advanced manual proxy server settings.
            IWebProxy proxy = WebRequest.GetSystemWebProxy();
            string proxyUrl = proxy.GetProxy(new Uri(url)).AbsoluteUri;
            WebProxy systemProxy = new WebProxy(proxyUrl);
            return systemProxy;
        }
        private bool IsProxyValid(IWebProxy proxy, string url)
        {
            bool result = true;
            WebRequest request = CreateRequest(url);
            WebResponse response = null;
            request.Proxy = proxy;
            try
            {
                response = request.GetResponse();
            }
            catch (WebException webException)
            {
                HttpWebResponse webResponse = webException.Response as HttpWebResponse;
                if (webResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
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
        private WebRequest CreateRequest(string url)
        {
            IHttpClient client = new HttpClient(new Uri(url));
            WebRequest request = client.CreateRequest();
            return request;
        }

        public bool AreCredentialsValid(ICredentials credentials, string url)
        {
            WebProxy proxy = GetSystemProxy(url);
            proxy.Credentials = credentials;
            return IsProxyValid(proxy, url);
        }
    }
}

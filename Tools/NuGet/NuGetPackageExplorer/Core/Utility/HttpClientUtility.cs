using System;
using NuGet.Repositories;
using System.Net;

namespace NuGet.Utility
{
    public static class HttpClientUtility
    {
        public static ProxyType GetProxyType(string url)
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
                        IWebProxy defaultProxy = WebRequest.GetSystemWebProxy();
                        if (IsProxyValid(defaultProxy,url))
                        {
                            return type;
                        }
                        break;
                    case ProxyType.IntegratedAuth:
                        WebProxy ntlmProxy = GetSystemProxy(url);
                        ntlmProxy.UseDefaultCredentials = true;
                        // Commenting out the Credentials setter based on the Remarks that can be found:
                        // http://msdn.microsoft.com/en-us/library/system.net.webrequest.usedefaultcredentials.aspx
                        // It is basically saying that it is best to set the UseDefaultCredentials for client applications
                        // and only use the Credentials property for middle tier applications such as ASP.NET applications.
                        //ntlmProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                        if (IsProxyValid(ntlmProxy,url))
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

        public static WebProxy GetSystemProxy(string url)
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

        static bool IsProxyValid(IWebProxy proxy,string url)
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

        public static bool CanConnect(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            bool result = true;
            WebRequest request = CreateRequest(url);
            try
            {
                WebResponse response = request.GetResponse();
                result = null != response && ((HttpWebResponse)response).StatusCode == HttpStatusCode.OK;
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        private static WebRequest CreateRequest(string url)
        {
            IHttpClient client = HttpClientFactory.Default.CreateClient(new Uri(url));
            WebRequest request = client.CreateRequest();
            return request;
        }
    }
}

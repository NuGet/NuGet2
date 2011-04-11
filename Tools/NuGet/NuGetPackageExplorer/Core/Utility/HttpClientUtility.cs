using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            ProxyType result = ProxyType.None;
            string[] proxyTypes = Enum.GetNames(typeof(ProxyType));
            
            for (int i = 0; i < proxyTypes.Length; i++)
            {
                ProxyType type = (ProxyType)Enum.Parse(typeof(ProxyType), proxyTypes[i]);
                switch (type)
                {
                    case ProxyType.None:
                        IWebProxy defaultProxy = HttpWebRequest.GetSystemWebProxy();
                        if (IsProxyValid(defaultProxy,url))
                        {
                            result = type;
                        }
                        break;
                    case ProxyType.NTLM:
                        WebProxy ntlmProxy = GetSystemProxy(url);
                        ntlmProxy.UseDefaultCredentials = true;
                        ntlmProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                        if (IsProxyValid(ntlmProxy,url))
                        {
                            result = type;
                        }
                        break;
                    case ProxyType.Basic:
                        // this is our last resort so we will simply return
                        // the ProxyType.Basic so that the user will be prompted
                        result = type;                        
                        break;
                }
            }
            return result;
        }

        public static WebProxy GetSystemProxy(string url)
        {
            IWebProxy proxy = HttpWebRequest.GetSystemWebProxy();
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
            WebResponse response = null;
            try
            {
                response = request.GetResponse();
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

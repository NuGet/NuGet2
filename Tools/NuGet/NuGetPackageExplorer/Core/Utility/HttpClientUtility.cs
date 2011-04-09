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
        public static bool IsProxyRequired(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            bool result = false;
            WebRequest request = CreateRequest(url);
            WebResponse response = null;
            try
            {
                response = request.GetResponse();
            }
            catch (WebException webException)
            {
                HttpWebResponse webResponse = webException.Response as HttpWebResponse;
                if (webResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                {
                    result = true;
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

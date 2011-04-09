using System;
using System.Net;
using System.Net.Cache;

namespace NuGet {
    public class HttpClient : IHttpClient {

        public string UserAgent {
            get;
            set;
        }
        public Uri Uri { get; set; }

        //public WebRequest CreateRequest(Uri uri) {
        //    WebRequest request = WebRequest.Create(uri);
        //    InitializeRequest(request);
        //    return request;
        //}

        public HttpClient(Uri uri)
        {
            if (null == uri)
            {
                throw new ArgumentNullException("uri");
            }
            Uri = uri;
        }

        public WebRequest CreateRequest()
        {
            WebRequest request = WebRequest.Create(Uri);
            InitializeRequest(request);
            return request;
        }

        public void InitializeRequest(WebRequest request) {
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null) {
                httpRequest.UserAgent = UserAgent;
                httpRequest.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            request.UseDefaultCredentials = true;
            if (request.Proxy != null) {
                // If we are going through a proxy then just set the default credentials
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }
        }

        public IHttpClient GetRedirectedClient(Uri uri) {
            IHttpClient client = new HttpClient(uri);
            WebRequest request = client.CreateRequest();
            using (WebResponse response = request.GetResponse())
            {
                if (null == response)
                {
                    return null;
                }
                return new HttpClient(response.ResponseUri);
            }
            //WebRequest request = CreateRequest(uri);
            //using (WebResponse response = request.GetResponse()) {
            //    if (response == null) {
            //        return null;
            //    }
            //    return response.ResponseUri;
            //}
        }
    }
}

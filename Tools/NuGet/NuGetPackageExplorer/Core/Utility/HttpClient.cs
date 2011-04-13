using System;
using System.Net;
using System.Net.Cache;
using System.Globalization;

namespace NuGet {
    public class HttpClient : IHttpClient {

        private const string _UserAgentPattern = "NuGet Package Explorer/{0} ({1})";
        string _userAgent = null;

        public HttpClient(Uri uri) : this(uri, null) { }
        public HttpClient(Uri uri, IWebProxy proxy)
        {
            if (null == uri)
            {
                throw new ArgumentNullException("uri");
            }
            Uri = uri;
            Proxy = proxy;

            Version version = typeof(GalleryServer).Assembly.GetNameSafe().Version;
            _userAgent = String.Format(CultureInfo.InvariantCulture, _UserAgentPattern, version, Environment.OSVersion);
        }

        public virtual WebRequest CreateRequest()
        {
            WebRequest request = WebRequest.Create(Uri);
            InitializeRequest(request);
            return request;
        }

        public void InitializeRequest(WebRequest request)
        {
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.UserAgent = UserAgent;
                httpRequest.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            request.Proxy = Proxy;
        }

        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

        public virtual Uri Uri { get; set; }
        public virtual IWebProxy Proxy { get; set; }

    }
}

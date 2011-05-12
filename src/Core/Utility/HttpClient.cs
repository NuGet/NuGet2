using System;
using System.IO;
using System.Net;

namespace NuGet {
    public class HttpClient : IHttpClient {
        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };
        public event EventHandler<WebRequestEventArgs> SendingRequest = delegate { };

        private static IProxyFinder _defaultProxyFinder = CreateFinder();

        private Uri _uri;

        private HttpClient() {
            ProxyFinder = DefaultProxyFinder;
        }

        public HttpClient(Uri uri)
            : this() {
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }

            _uri = uri;
        }

        public string UserAgent {
            get;
            set;
        }

        public virtual Uri Uri {
            get {
                return _uri;
            }
            set {
                _uri = value;
            }
        }

        public IWebProxy Proxy {
            get;
            set;
        }

        public IProxyFinder ProxyFinder {
            get;
            set;
        }

        public bool AcceptCompression {
            get;
            set;
        }

        public static IProxyFinder DefaultProxyFinder {
            get {
                return _defaultProxyFinder;
            }
            set {
                _defaultProxyFinder = value;
            }
        }

        public virtual WebRequest CreateRequest() {
            WebRequest request = WebRequest.Create(Uri);
            InitializeRequest(request);
            return request;
        }

        public void InitializeRequest(WebRequest request) {
            var httpRequest = request as HttpWebRequest;
            if(httpRequest != null) {
                httpRequest.UserAgent = UserAgent;
                if(AcceptCompression) {
                    httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                }
            }
            // Let's try and see if the given proxy is set to null and if it is then we'll try to ask
            // The ProxyFinder to try and auto detect the proxy for us.
            // By default if the user has not set the proxy then the proxy should be set to the WebRequest.DefaultWebProxy
            // If the user has manually set or passed in a proxy as Null then we probably don't want to auto detect.
            if(ProxyFinder != null) {
                Proxy = ProxyFinder.GetProxy(Uri);
            }
            // Set the request proxy to the set Proxy property instance.
            request.Proxy = Proxy;
            // giving clients a chance to examine/modify the request object before the request actually goes out.
            RaiseSendingRequest(request);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public byte[] DownloadData() {
            const int ChunkSize = 1024 * 4; // 4KB

            byte[] buffer = null;

            // we don't want to enable compression when downloading packages
            WebRequest request = CreateRequest();
            using (var response = request.GetResponse()) {

                // total response length
                int length = (int)response.ContentLength;
                buffer = new byte[length];

                // We read the response stream chunk by chunk (each chunk is 4KB). 
                // After reading each chunk, we report the progress based on the total number bytes read so far.
                int totalReadSoFar = 0;
                using (Stream stream = response.GetResponseStream()) {
                    while (totalReadSoFar < length) {
                        int bytesRead = stream.Read(buffer, totalReadSoFar, Math.Min(length - totalReadSoFar, ChunkSize));
                        if (bytesRead == 0) {
                            break;
                        }
                        else {
                            totalReadSoFar += bytesRead;
                            OnProgressAvailable((totalReadSoFar * 100) / length);
                        }
                    }
                }
            }

            return buffer;
        }

        private void OnProgressAvailable(int percentage) {
            ProgressAvailable(this, new ProgressEventArgs(percentage));
        }

        private void RaiseSendingRequest(WebRequest webRequest) {
            SendingRequest(this, new WebRequestEventArgs(webRequest));
        }

        /// <summary>
        /// Initialize the static instance of the IProxyFinder that is going to be
        /// used as the default instance for most of the HttpClient instances.
        /// </summary>
        /// <returns></returns>
        private static IProxyFinder CreateFinder() {
            var proxyFinder = new ProxyFinder();
            proxyFinder.RegisterProvider(new IntegratedCredentialProvider());
            return proxyFinder;
        }
    }
}
using System;
using System.IO;
using System.Net;

namespace NuGet {
    public class HttpClient : IHttpClient {
        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };
        public event EventHandler<WebRequestEventArgs> SendingRequest = delegate { };

        public string UserAgent {
            get;
            set;
        }

        public WebRequest CreateRequest(Uri uri, bool acceptCompression) {
            WebRequest request = WebRequest.Create(uri);
            InitializeRequest(request, acceptCompression);
            return request;
        }

        public void InitializeRequest(WebRequest request, bool acceptCompression) {
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null) {
                httpRequest.UserAgent = UserAgent;
                if (acceptCompression) {
                    httpRequest.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                    httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                }
            }

            request.UseDefaultCredentials = true;
            if (request.Proxy != null) {
                // If we are going through a proxy then just set the default credentials
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }

            // giving clients a chance to examine/modify the request object before the request actually goes out.
            RaiseSendingRequest(request);
        }

        public Uri GetRedirectedUri(Uri uri) {
            WebRequest request = CreateRequest(uri, acceptCompression: false);
            using (WebResponse response = request.GetResponse()) {
                if (response == null) {
                    return null;
                }
                return response.ResponseUri;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public byte[] DownloadData(Uri uri) {
            const int ChunkSize = 1024 * 4; // 4KB

            byte[] buffer = null;

            // we don't want to enable compression when downloading packages
            WebRequest request = CreateRequest(uri, acceptCompression: false);
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
    }
}
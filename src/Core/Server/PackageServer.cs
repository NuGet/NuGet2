using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using Microsoft.Internal.Web.Utils;
using System.Diagnostics.CodeAnalysis;

namespace NuGet {
    public class PackageServer : IPackageServer, IProgressProvider {
        private const string CreatePackageService = "PackageFiles";
        private const string PackageService = "Packages";
        private const string PublichPackageService = "PublishedPackages/Publish";

        private readonly Lazy<IHttpClient> _galleryClient;
        private readonly string _source;
        private readonly string _userAgent;

        public event EventHandler<ProgressEventArgs> ProgressAvailable;

        public PackageServer(string source, string userAgent) {
            if (String.IsNullOrEmpty(source)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "source");
            }
            _source = source.TrimEnd('/');
            _galleryClient = new Lazy<IHttpClient>(() => CreateClient(source));
            _userAgent = userAgent;
        }

        public string Source {
            get {
                return _source;
            }
        }

        public void CreatePackage(string apiKey, Stream packageStream) {
            const int chunkSize = 1024 * 4; // 4KB

            var action = String.Format("{0}/{1}/nupkg", CreatePackageService, apiKey);
            var request = CreateRequest(action, "POST", "application/octet-stream");

            // Set the timeout to the same as the read write timeout (5 mins is the default)
            request.Timeout = request.ReadWriteTimeout;

            byte[] buffer = packageStream.ReadAllBytes();
            request.ContentLength = buffer.Length;

            OnProgressAvailable(0);
            int offset = 0;
            using (var requestStream = request.GetRequestStream()) {
                while (offset < buffer.Length) {
                    int count = Math.Min(buffer.Length - offset, chunkSize);
                    requestStream.Write(buffer, offset, count);
                    offset += count;
                    int percentage = (offset * 100) / buffer.Length;
                    if (percentage < 100) {
                        OnProgressAvailable(percentage);
                    }
                }
            }

            OnProgressAvailable(100);

            GetResponse(request);
        }

        public void PublishPackage(string apiKey, string packageId, string packageVersion) {
            var request = CreateRequest(PublichPackageService, "POST", "application/json");

            using (Stream requestStream = request.GetRequestStream()) {
                var data = new PublishData {
                    Key = apiKey,
                    Id = packageId,
                    Version = packageVersion
                };

                var jsonSerializer = new DataContractJsonSerializer(typeof(PublishData));
                jsonSerializer.WriteObject(requestStream, data);
            }

            GetResponse(request);
        }

        public void DeletePackage(string apiKey, string packageId, string packageVersion) {
            var action = String.Join("/", PackageService, apiKey, packageId, packageVersion);
            var request = CreateRequest(action, "DELETE", "text/html");
            request.ContentLength = 0;

            GetResponse(request);
        }

        private HttpWebRequest CreateRequest(string action, string method, string contentType) {
            var actionUrl = _galleryClient.Value.Uri.OriginalString + '/' + action;
            var actionClient = new HttpClient(new Uri(actionUrl));
            var request = actionClient.CreateRequest() as HttpWebRequest;
            request.ContentType = contentType;
            request.Method = method;
            
            if (!String.IsNullOrEmpty(_userAgent)) {
                request.UserAgent = HttpUtility.CreateUserAgentString(_userAgent);
            }

            return request;
        }

        private static WebResponse GetResponse(WebRequest request) {
            try {
                return request.GetResponse();
            }
            catch (WebException e) {
                string errorMessage = String.Empty;
                var response = (HttpWebResponse)e.Response;
                using (var stream = response.GetResponseStream()) {
                    errorMessage = stream.ReadToEnd();
                }

                throw new WebException(errorMessage, e, e.Status, e.Response);
            }
        }

        private void OnProgressAvailable(int percentage) {
            if (ProgressAvailable != null) {
                ProgressAvailable(this, new ProgressEventArgs(percentage));
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "uri", Justification = "Reading the Uri property causes a 403 if something failed.")]
        private static IHttpClient CreateClient(string source) {
            IHttpClient client = null;
            try {
                client = new RedirectedHttpClient(new Uri(source));
                // force the client to load the Uri so that we can catch the 403 - Forbidden: exception from the
                // server and just return the proper IHttpClient.
                var uri = client.Uri;
            }
            catch (WebException e) {
                if (e.Status == WebExceptionStatus.Timeout) {
                    // If we got a timeout error then throw it up to the consumer
                    // because we are not able to connect to the gallery server.
                    throw;
                }
                // Since we did not time out then it must be that we are getting the 403 error
                // which is valid since this Url is going to be used for Post and Delete actions
                // and we are simply trying to perform a GET to retrieve the server Url from the fw link.
                // So let's create a new IHttpClient that is going to against this new Url
                client = new HttpClient(e.Response.ResponseUri);
            }
            return client;
        }
    }
}

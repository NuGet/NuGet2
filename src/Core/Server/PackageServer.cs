using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using Microsoft.Internal.Web.Utils;

namespace NuGet {
    public class PackageServer : IPackageServer {
        private const string CreatePackageService = "PackageFiles";
        private const string PackageService = "Packages";
        private const string PublishPackageService = "PublishedPackages/Publish";

        private readonly Lazy<Uri> _baseUri;
        private readonly string _source;
        private readonly string _userAgent;

        public PackageServer(string source, string userAgent) {
            if (String.IsNullOrEmpty(source)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "source");
            }
            _source = source;
            _userAgent = userAgent;
            _baseUri = new Lazy<Uri>(ResolveBaseUrl);
        }

        public string Source {
            get { return _source; }
        }

        public void CreatePackage(string apiKey, Stream packageStream) {
            var url = String.Join("/", CreatePackageService, apiKey, "nupkg");
            HttpWebRequest request = CreateRequest(url, "POST", "application/octet-stream");

            // Set the timeout to the same as the read write timeout (5 mins is the default)
            request.Timeout = request.ReadWriteTimeout;
            request.ContentLength = packageStream.Length;

            using (Stream requestStream = request.GetRequestStream()) {
                packageStream.CopyTo(requestStream);
            }

            GetResponse(request);
        }

        public void PublishPackage(string apiKey, string packageId, string packageVersion) {
            HttpWebRequest request = CreateRequest(PublishPackageService, "POST", "application/json");

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
            var url = String.Join("/", PackageService, apiKey, packageId, packageVersion);
            var request = CreateRequest(url, "DELETE", "text/html");
            request.ContentLength = 0;

            GetResponse(request);
        }
 
        private HttpWebRequest CreateRequest(string url, string method, string contentType) {
            var uri = new Uri(_baseUri.Value, url);
            var client = new HttpClient(uri);
            var request = (HttpWebRequest)client.CreateRequest();
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
                if (e.Response == null) {
                    throw;
                }

                var response = (HttpWebResponse)e.Response;
                string errorMessage = String.Empty;
                using (var stream = response.GetResponseStream()) {
                    errorMessage = stream.ReadToEnd().Trim();
                }

                throw new WebException(errorMessage, e, e.Status, e.Response);
            }
        }

        private Uri ResolveBaseUrl() {
            var client = new RedirectedHttpClient(new Uri(Source));
            return EnsureTrailingSlash(client.Uri);
        }

        private static Uri EnsureTrailingSlash(Uri uri) {
            string value = uri.OriginalString;
            if (!value.EndsWith("/", StringComparison.OrdinalIgnoreCase)) {
                value += "/";
            }
            return new Uri(value);
        }
    }
}

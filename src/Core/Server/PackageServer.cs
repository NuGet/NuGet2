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

            HttpClient client = GetClient(url, "POST", "application/octet-stream");

            client.SendingRequest += (sender, e) => {
                var request = (HttpWebRequest)e.Request;

                // Set the timeout to the same as the read write timeout (5 mins is the default)
                request.Timeout = request.ReadWriteTimeout;
                request.ContentLength = packageStream.Length;

                using (Stream requestStream = request.GetRequestStream()) {
                    packageStream.CopyTo(requestStream);
                }
            };

            EnsureSuccessfulResponse(client);
        }

        public void PublishPackage(string apiKey, string packageId, string packageVersion) {
            HttpClient client = GetClient(PublishPackageService, "POST", "application/json");

            client.SendingRequest += (sender, e) => {
                using (Stream requestStream = e.Request.GetRequestStream()) {
                    var data = new PublishData {
                        Key = apiKey,
                        Id = packageId,
                        Version = packageVersion
                    };

                    var jsonSerializer = new DataContractJsonSerializer(typeof(PublishData));
                    jsonSerializer.WriteObject(requestStream, data);
                }
            };

            EnsureSuccessfulResponse(client);
        }

        public void DeletePackage(string apiKey, string packageId, string packageVersion) {
            var url = String.Join("/", PackageService, apiKey, packageId, packageVersion);

            HttpClient client = GetClient(url, "DELETE", "text/html");

            client.SendingRequest += (sender, e) => {
                e.Request.ContentLength = 0;
            };

            EnsureSuccessfulResponse(client);
        }

        private HttpClient GetClient(string url, string method, string contentType) {
            var uri = new Uri(_baseUri.Value, url);
            var client = new HttpClient(uri);
            client.ContentType = contentType;
            client.Method = method;

            if (!String.IsNullOrEmpty(_userAgent)) {
                client.UserAgent = HttpUtility.CreateUserAgentString(_userAgent);
            }

            return client;
        }

        private static void EnsureSuccessfulResponse(HttpClient client) {
            WebResponse response = null;
            try {
                response = client.GetResponse();
            }
            catch (WebException e) {
                if (e.Response == null) {
                    throw;
                }

                response = e.Response;

                var httpResponse = (HttpWebResponse)e.Response;
                string errorMessage = String.Empty;
                using (var stream = httpResponse.GetResponseStream()) {
                    errorMessage = stream.ReadToEnd().Trim();
                }

                throw new WebException(errorMessage, e, e.Status, e.Response);
            }
            finally {
                if (response != null) {
                    response.Close();
                    response = null;
                }
            }
        }

        private Uri ResolveBaseUrl() {
            Uri uri = null;

            try {
                var client = new RedirectedHttpClient(new Uri(Source));
                uri = client.Uri;
            }
            catch (WebException ex) {
                var response = (HttpWebResponse)ex.Response;
                if (response == null) {
                    throw;
                }

                uri = response.ResponseUri;
            }

            return EnsureTrailingSlash(uri);
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

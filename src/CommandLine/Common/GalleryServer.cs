using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.Internal.Web.Utils;

namespace NuGet.Common {
    public class GalleryServer : IProgressProvider {
        public static readonly string DefaultSymbolServerUrl = "http://nuget.gw.symbolsource.org/Public/NuGet";
        public static readonly string DefaultGalleryServerUrl = "http://go.microsoft.com/fwlink/?LinkID=207106";
        private const string CreatePackageService = "PackageFiles";
        private const string PackageService = "Packages";
        private const string PublishPackageService = "PublishedPackages/Publish";

        private const string _UserAgentClient = "NuGet Command Line";
        private readonly Lazy<IHttpClient> _galleryClient;
        private readonly string _galleryServerUrl;

        public event EventHandler<ProgressEventArgs> ProgressAvailable;

        public GalleryServer()
            : this(DefaultGalleryServerUrl) {
        }

        public GalleryServer(string galleryServerUrl) {
            if (String.IsNullOrEmpty(galleryServerUrl)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "galleryServerUrl");
            }
            _galleryServerUrl = galleryServerUrl;
            _galleryClient = new Lazy<IHttpClient>(EnsureClient);
        }

        public IHttpClient EnsureClient() {
            IHttpClient client = null;
            try {
                client = new RedirectedHttpClient(new Uri(_galleryServerUrl));
                // force the client to load the Uri so that we can catch the 403 - Forbidden: exception from the
                // server and just return the proper IHttpClient.
                var uri = client.Uri;
            }
            catch (WebException e) {
                if (e.Status == WebExceptionStatus.Timeout || e.Response == null) {
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

        public void CreatePackage(string apiKey, string externalUrl) {
            var action = String.Format("{0}/CreateFromExternalUrl", CreatePackageService);
            var request = CreateRequest(action, "POST", "application/json");

            using (Stream requestStream = request.GetRequestStream()) {
                var data = new CreateFromExternalUrlData {
                    Key = apiKey,
                    FileExtension = "nupkg",
                    ExternalPackageUrl = externalUrl
                };

                var jsonSerializer = new DataContractJsonSerializer(typeof(CreateFromExternalUrlData));
                jsonSerializer.WriteObject(requestStream, data);
            }

            GetResponse(request);

        }

        public void PublishPackage(string apiKey, string packageID, string packageVersion) {
            var request = CreateRequest(PublishPackageService, "POST", "application/json");

            using (Stream requestStream = request.GetRequestStream()) {
                var data = new PublishData {
                    Key = apiKey,
                    Id = packageID,
                    Version = packageVersion
                };

                var jsonSerializer = new DataContractJsonSerializer(typeof(PublishData));
                jsonSerializer.WriteObject(requestStream, data);
            }

            GetResponse(request);
        }

        public void DeletePackage(string apiKey, string packageID, string packageVersion) {
            var action = String.Format("{0}/{1}/{2}/{3}", PackageService, apiKey, packageID, packageVersion);
            var request = CreateRequest(action, "DELETE", "text/html");
            request.ContentLength = 0;

            GetResponse(request);
        }

        public void RatePackage(string packageID, string packageVersion, string rating) {
            var action = String.Format("{0}/{1}", PackageService, "RatePackage");
            var request = CreateRequest(action, "POST", "application/json");

            using (Stream requestStream = request.GetRequestStream()) {
                var data = new RatePackageData {
                    Id = packageID,
                    Version = packageVersion,
                    Rating = rating
                };

                var jsonSerializer = new DataContractJsonSerializer(typeof(RatePackageData));
                jsonSerializer.WriteObject(requestStream, data);
            }

            GetResponse(request);
        }

        private void OnProgressAvailable(int percentage) {
            if (ProgressAvailable != null) {
                ProgressAvailable(this, new ProgressEventArgs(percentage));
            }
        }

        private HttpWebRequest CreateRequest(string action, string method, string contentType) {
            var actionUrl = String.Format("{0}/{1}", _galleryClient.Value.Uri, action);
            var actionClient = new HttpClient(new Uri(actionUrl));
            var request = actionClient.CreateRequest() as HttpWebRequest;
            request.ContentType = contentType;
            request.Method = method;
            request.UserAgent = HttpUtility.CreateUserAgentString(_UserAgentClient);
            return request;
        }

        private WebResponse GetResponse(WebRequest request) {
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
                    errorMessage = stream.ReadToEnd();
                }

                throw new WebException(errorMessage, e, e.Status, e.Response);
            }
        }

        [DataContract]
        public class PublishData {
            [DataMember(Name = "key")]
            public string Key { get; set; }

            [DataMember(Name = "id")]
            public string Id { get; set; }

            [DataMember(Name = "version")]
            public string Version { get; set; }
        }

        [DataContract]
        public class CreateFromExternalUrlData {
            [DataMember(Name = "key")]
            public string Key { get; set; }

            [DataMember(Name = "fileExtension")]
            public string FileExtension { get; set; }

            [DataMember(Name = "externalPackageUrl")]
            public string ExternalPackageUrl { get; set; }
        }

        [DataContract]
        public class RatePackageData {
            [DataMember(Name = "id")]
            public string Id { get; set; }

            [DataMember(Name = "version")]
            public string Version { get; set; }

            [DataMember(Name = "rating")]
            public string Rating { get; set; }
        }
    }
}

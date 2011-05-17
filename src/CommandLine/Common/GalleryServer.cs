using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace NuGet.Common {
    public class GalleryServer {
        public static readonly string DefaultSymbolServerUrl = "http://nuget.gw.symbolsource.org/Public/NuGet";
        public static readonly string DefaultGalleryServerUrl = "http://go.microsoft.com/fwlink/?LinkID=207106";
        private const string CreatePackageService = "PackageFiles";
        private const string PackageService = "Packages";
        private const string PublichPackageService = "PublishedPackages/Publish";

        private const string _UserAgentClient = "NuGet Command Line";
        private IHttpClient _galleryClient;

        public GalleryServer()
            : this(DefaultGalleryServerUrl) {
        }

        public GalleryServer(string galleryServerUrl) {
            if (string.IsNullOrEmpty(galleryServerUrl)) {
                throw new ArgumentNullException("galleryServerUrl");
            }
            _galleryClient = new RedirectedHttpClient(new Uri(galleryServerUrl));
        }

        public void CreatePackage(string apiKey, Stream package) {
            var action = String.Format("{0}/{1}/nupkg", CreatePackageService, apiKey);
            var request = CreateRequest(action, "POST", "application/octet-stream");

            byte[] file = package.ReadAllBytes();
            request.ContentLength = file.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(file, 0, file.Length);

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
            var request = CreateRequest(PublichPackageService, "POST", "application/json");

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

        private HttpWebRequest CreateRequest(string action, string method, string contentType) {
            var actionUrl = string.Format("{0}/{1}", _galleryClient.Uri, action);
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
                string errorMessage = String.Empty;
                var response = (HttpWebResponse)e.Response;
                using (var stream = response.GetResponseStream()) {
                    errorMessage = stream.ReadToEnd();
                }

                throw new WebException(errorMessage, e, e.Status, e.Response);
            }
        }

        private string GetSafeRedirectedUri(string uri) {
            WebRequest request = WebRequest.Create(uri);
            try {
                WebResponse response = request.GetResponse();
                if (response == null) {
                    return null;
                }
                return response.ResponseUri.ToString();
            }
            catch (WebException e) {
                return e.Response.ResponseUri.ToString(); ;
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

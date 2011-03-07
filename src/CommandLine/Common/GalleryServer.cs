using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace NuGet.Common {   
    public class GalleryServer {
        public static string DefaultGalleryServerUrl = "http://go.microsoft.com/fwlink/?LinkID=207106";
        private const string CreatePackageService = "PackageFiles";
        private const string PackageService = "Packages";
        private const string PublichPackageService = "PublishedPackages/Publish";

        //REVIEW: What should be the User agent
        private const string _UserAgentPattern = "NuGet/{0} ({1})";
        
        private string _baseGalleryServerUrl;
        private string _userAgent;



        public GalleryServer()
            : this(DefaultGalleryServerUrl) {
        }

        public GalleryServer(string galleryServerUrl) {
            _baseGalleryServerUrl = GetSafeRedirectedUri(galleryServerUrl);

            var version = typeof(GalleryServer).Assembly.GetNameSafe().Version;
            _userAgent = String.Format(CultureInfo.InvariantCulture, _UserAgentPattern, version, Environment.OSVersion);
        }

        public void CreatePackage(string apiKey, Stream package) {
            var url = new Uri(String.Format("{0}/{1}/{2}/nupkg", _baseGalleryServerUrl, CreatePackageService, apiKey));
            var request = CreateRequest(url, "POST", "application/octet-stream");

            byte[] file = package.ReadAllBytes();
            request.ContentLength = file.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(file, 0, file.Length);

            GetResponse(request);
        }

        public void CreatePackage(string apiKey, string externalUrl) {
            var url = new Uri(String.Format("{0}/{1}/CreateFromExternalUrl", _baseGalleryServerUrl, CreatePackageService));
            var request = CreateRequest(url, "POST", "application/json");

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
            var url = new Uri(String.Format("{0}/{1}", _baseGalleryServerUrl, PublichPackageService));
            var request = CreateRequest(url, "POST", "application/json");

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
            var url = new Uri(String.Format("{0}/{1}/{2}/{3}/{4}", _baseGalleryServerUrl, PackageService, apiKey, packageID, packageVersion));
            var request = CreateRequest(url, "DELETE", "text/html");
            request.ContentLength = 0;

            GetResponse(request);
        }

        public void RatePackage(string packageID, string packageVersion, string rating) {
            var url = new Uri(String.Format("{0}/{1}/{2}", _baseGalleryServerUrl, PackageService, "RatePackage"));
            var request = CreateRequest(url, "POST", "application/json");

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

        private HttpWebRequest CreateRequest(Uri url, string method, string contentType) {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = contentType;
            request.Method = method;
            request.UserAgent = _userAgent;
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

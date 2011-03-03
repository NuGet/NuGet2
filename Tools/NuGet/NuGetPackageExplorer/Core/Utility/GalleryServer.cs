using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace NuGet {   
    public class GalleryServer {
        private const string DefaultGalleryServerUrl = "http://go.microsoft.com/fwlink/?LinkID=207106";
        private const string CreatePackageService = "PackageFiles";
        private const string PackageService = "Packages";
        private const string PublishPackageService = "PublishedPackages/Publish";

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

        public void CreatePackage(string apiKey, Stream packageStream, IObserver<int> progressObserver, IPackageMetadata metadata = null) {

            var state = new PublishState {
                PublishKey = apiKey,
                PackageMetadata = metadata, 
                ProgressObserver = progressObserver
            };

            var url = new Uri(String.Format("{0}/{1}/{2}/nupkg", _baseGalleryServerUrl, CreatePackageService, apiKey));

            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.ContentType] = "application/octet-stream";
            client.UploadProgressChanged += OnUploadProgressChanged;
            client.UploadDataCompleted += OnCreatePackageCompleted;
            client.UploadDataAsync(url, "POST", packageStream.ReadAllBytes(), state);
        }

        private void PublishPackage(PublishState state) {
            var url = new Uri(String.Format("{0}/{1}", _baseGalleryServerUrl, PublishPackageService));

            using (Stream requestStream = new MemoryStream()) {
                var data = new PublishData {
                    Key = state.PublishKey,
                    Id = state.PackageMetadata.Id,
                    Version = state.PackageMetadata.Version.ToString()
                };

                var jsonSerializer = new DataContractJsonSerializer(typeof(PublishData));
                jsonSerializer.WriteObject(requestStream, data);
                requestStream.Seek(0, SeekOrigin.Begin);

                WebClient client = new WebClient();
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.UploadProgressChanged += OnUploadProgressChanged;
                client.UploadDataCompleted += OnPublishPackageCompleted;
                client.UploadDataAsync(url, "POST", requestStream.ReadAllBytes(), state);
            }
        }

        private void OnCreatePackageCompleted(object sender, UploadDataCompletedEventArgs e) {
            var state = (PublishState) e.UserState;
            if (e.Error != null) {
                Exception error = e.Error;

                WebException webException = e.Error as WebException;
                if (webException != null) {
                    var response = (HttpWebResponse) webException.Response;
                    if (response.StatusCode == HttpStatusCode.InternalServerError) {
                        // real error message is contained inside the response body
                        using (Stream stream = response.GetResponseStream()) {
                            string errorMessage = stream.ReadToEnd();
                            error = new WebException(errorMessage, webException, webException.Status,
                                                     webException.Response);
                        }
                    }
                }
                
                state.ProgressObserver.OnError(error);
            }
            else if (!e.Cancelled) {
                if (state.PackageMetadata != null) {
                    PublishPackage(state);
                }
                else {
                    state.ProgressObserver.OnCompleted();
                }
            }
        }

        private void OnPublishPackageCompleted(object sender, UploadDataCompletedEventArgs e) {
            var state = (PublishState)e.UserState;
            if (e.Error != null) {
                Exception error = e.Error;

                WebException webException = e.Error as WebException;
                if (webException != null) {
                    // real error message is contained inside the response body
                    using (Stream stream = webException.Response.GetResponseStream()) {
                        string errorMessage = stream.ReadToEnd();
                        error = new WebException(errorMessage, webException, webException.Status, webException.Response);
                    }
                }

                state.ProgressObserver.OnError(error);
            }
            else if (!e.Cancelled) {
                state.ProgressObserver.OnCompleted();
            }
        }

        private void OnUploadProgressChanged(object sender, UploadProgressChangedEventArgs e) {
            var state = (PublishState)e.UserState;
            state.ProgressObserver.OnNext(e.ProgressPercentage);
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

        private class PublishState {
            public string PublishKey { get; set; }
            public IObserver<int> ProgressObserver { get; set; }
            public IPackageMetadata PackageMetadata { get; set; }
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
    }
}
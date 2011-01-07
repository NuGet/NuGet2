using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Globalization;
using System.IO;

namespace NuGet.Common {


    /* CreatePackage (stream & URL)
     * PublishPackage
     * UpdatePackage
     * DeletePackage
     * RatePackage
     * DeleteScreenshot */


    
    public class GalleryServer {
        
        private const string _CreatePackageService = "PackageFiles";
        private const string _PublichPackageService = "PublishedPackages";
        private const string _UserAgentPattern = "Nuget Gallery API/{0} ({1})";

        private string _baseGalleryServerUrl;
        private string _userAgent;


        public GalleryServer(string galleryServerUrl) {
            _baseGalleryServerUrl = GetSafeRedirectedUri(galleryServerUrl);

            var version = typeof(GalleryServer).Assembly.GetNameSafe().Version;
            _userAgent = String.Format(CultureInfo.InvariantCulture, _UserAgentPattern, version, Environment.OSVersion);
        }

        
        public void CreatePackage(string apiKey, Stream Package) {
            var url = new Uri(String.Format("{0}/{1}/{2}/nupkg", _baseGalleryServerUrl, _CreatePackageService, apiKey));

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/octet-stream";
            request.Method = "POST";
            request.UserAgent = _userAgent;

            byte[] file = Package.ReadAllBytes();
            request.ContentLength = file.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(file, 0, file.Length);

            GetResponse(request);
        }

        public void CreatePackage(string apiKey, string PackageUrl) {

            var url = new Uri(String.Format("{0}/{1}/CreateFromExternalUrl", _baseGalleryServerUrl, _CreatePackageService));

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.UserAgent = _userAgent;
            request.ContentLength = 0;


            GetResponse(request);

            

        }

        public void PublishPackage(string apiKey, string packageID, string packageVersion) {
            var url = new Uri(String.Format("{0}/{1}/{2}/{3}/{4}", _baseGalleryServerUrl, _PublichPackageService, apiKey, packageID, packageVersion));

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = _userAgent;

            GetResponse(request);
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

    }
}

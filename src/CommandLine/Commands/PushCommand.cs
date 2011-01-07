using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using NuGet.Common;

namespace NuGet.Commands {

    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "push", "PushCommandDescription",
        MinArgs = 2, MaxArgs = 2, UsageDescriptionResourceName = "PushCommandUsageDescription",
        UsageSummaryResourceName = "PushCommandUsageSummary")]
    public class PushCommand : ICommand {
        private const string _CreatePackageService = "PackageFiles";
        private const string _PublichPackageService = "PublishedPackages/Publish";
        private const string _UserAgentPattern = "CommandLine/{0} ({1})";

        private string _apiKey;
        private string _packagePath;
        private string _userAgent;
        private string _baseGalleryServerUrl;

        public List<string> Arguments { get; set; }

        public IConsole Console { get; set; }

        [Option(typeof(NuGetResources), "PushCommandPublishDescription", AltName = "pub")]
        public bool Publish { get; set; }

        [Option(typeof(NuGetResources), "PushCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        [ImportingConstructor]
        public PushCommand(IConsole console) {
            Console = console;
            Publish = true;
        }

        public void Execute() {
            //Frist argument should be the package
            _packagePath = Arguments[0];
            //Second argument should be the API Key
            _apiKey = Arguments[1];

            var client = new HttpClient();
            

            if (String.IsNullOrEmpty(Source)) {
                throw new CommandLineException(NuGetResources.PushCommandNoSourceError);
            }
            _baseGalleryServerUrl = GetSafeRedirectedUri(Source);

            var version = typeof(PushCommand).Assembly.GetNameSafe().Version;
            _userAgent = String.Format(CultureInfo.InvariantCulture, _UserAgentPattern, version, Environment.OSVersion);

            PushPackage();
        }

        private void PushPackage() {

            var url = new Uri(String.Format("{0}/{1}/{2}/nupkg", _baseGalleryServerUrl, _CreatePackageService, _apiKey));

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/octet-stream";
            request.Method = "POST";
            request.UserAgent = _userAgent;

            ZipPackage pkg = new ZipPackage(_packagePath);

            using (Stream pkgStream = pkg.GetStream()) {
                byte[] file = pkgStream.ReadAllBytes();
                request.ContentLength = file.Length;
                var requestStream = request.GetRequestStream();
                requestStream.Write(file, 0, file.Length);
            }

            Console.WriteLine(NuGetResources.PushCommandCreatingPackage, pkg.Id, pkg.Version);

            var response = SafeGetResponse(request);

            if (response.StatusCode != HttpStatusCode.OK) {
                string errorMessage = String.Empty;
                using (var stream = response.GetResponseStream()) {
                    errorMessage = stream.ReadToEnd();
                }

                throw new CommandLineException(NuGetResources.PushCommandInvalidResponse, response.StatusCode, errorMessage);
            }

            Console.WriteLine(NuGetResources.PushCommandPackageCreated);

            if (Publish) {
                PublishPackage(pkg.Id, pkg.Version.ToString());
            }

        }

        private void PublishPackage(string id, string version) {
            Console.WriteLine(NuGetResources.PushCommandPublishingPackage, id, version);

            var url = new Uri(String.Format("{0}/{1}", _baseGalleryServerUrl, _PublichPackageService));

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.UserAgent = _userAgent;

            using (Stream requestStream = request.GetRequestStream()) {
                var data = new PublishData {
                    Key = _apiKey,
                    Id = id,
                    Version = version
                };

                var jsonSerializer = new DataContractJsonSerializer(typeof(PublishData));
                jsonSerializer.WriteObject(requestStream, data);
            }

            var response = SafeGetResponse(request);

            if (response.StatusCode != HttpStatusCode.OK) {
                string errorMessage = String.Empty;
                using (var stream = response.GetResponseStream()) {
                    errorMessage = stream.ReadToEnd();
                }

                throw new CommandLineException(NuGetResources.PushCommandInvalidResponse, response.StatusCode, errorMessage);
            }
            Console.WriteLine(NuGetResources.PushCommandPackagePublished);
        }

        private HttpWebResponse SafeGetResponse(HttpWebRequest request) {
            try {
                return (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e) {
                return (HttpWebResponse)e.Response;
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
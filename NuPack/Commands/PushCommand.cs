using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Common;
using System.Net;

namespace NuGet.Commands {

    //Push c:\foo\p.nupkg asd-dwf-123 -Publish-

    [Export(typeof(ICommand))]
    [Command("Push", "Push package to feed", MinArgs = 2, MaxArgs = 2)]
    public class PushCommand : ICommand {
        private const string _baseFeedUrl = "http://localhost/GalleryServer";
        private const string _createPackageService = "PackageFiles";
        private const string _publichPackageService = "PublishedPackages";

        private string _apiKey;
        private string _packagePath;

        public List<string> Arguments { get; set; }

        public IConsole Console { get; set; }

        [Option("Should publish package, be default true", AltName = "p")]
        public bool Publish { get; set; }

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

            PushPackage();

            if (!Publish) {
                Console.WriteLine("Your Package has been created in the Gallery Server but not published");
            }
            else {
                Console.WriteLine("Your package has been created and published on the Gallery Server");
            }


        }

        private void PushPackage() {
            Console.WriteLine("Creating and entry for your package...");
            //Create the Package
            var url = new Uri(string.Format("{0}/{1}/{2}/nupkg", _baseFeedUrl, _createPackageService, _apiKey));

            var request = WebRequest.Create(url);
            request.ContentType = "application/octet-stream";
            request.Method = "POST";

            ZipPackage pkg = new ZipPackage(_packagePath);

            using (Stream pkgStream = pkg.GetStream()) {
                byte[] file = pkgStream.ReadAllBytes();
                request.ContentLength = file.Length;
                var requestStream = request.GetRequestStream();
                requestStream.Write(file, 0, file.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            //TODO: What other codes does the server use?
            if (response.StatusCode != HttpStatusCode.OK) {
                throw new CommandLineException("There was a problem and your package was not uploaded. Status Code {0}", response.StatusCode);
            }


            //Publish the created package
            if (Publish) {
                PublishPackage(pkg.Id, pkg.Version.ToString());
            }

        }


        private void PublishPackage(string id, string version) {
            Console.WriteLine("Publishing your package...");

            var url = new Uri(string.Format("{0}/{1}/{2}/{3}/{4}", _baseFeedUrl, _publichPackageService, _apiKey, id, version));

            var request = WebRequest.Create(url);
            request.Method = "GET";

            var response = (HttpWebResponse)request.GetResponse();

            //TODO: What other codes does the server use?
            if (response.StatusCode != HttpStatusCode.OK) {
                throw new CommandLineException("There was a problem and your package was not uploaded. Status Code {0}", response.StatusCode);
            }
        }
    }
}

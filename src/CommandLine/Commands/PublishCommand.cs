using System;
using System.ComponentModel.Composition;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "publish", "PublishCommandDescription",
        MinArgs = 3, MaxArgs = 3, UsageDescriptionResourceName = "PublishCommandUsageDescription",
        UsageSummaryResourceName = "PublishCommandUsageSummary")]
    public class PublishCommand : Command {
        
        private string _apiKey;
        private string _packageId;
        private string _packageVersion;

        [Option(typeof(NuGetResources), "PublishCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }


        public override void ExecuteCommand() {
            //Frist argument should be the package ID
            _packageId = Arguments[0];
            //Second argument should be the package Version
            _packageVersion = Arguments[1];
            //Third argument should be the API Key
            _apiKey = Arguments[2];


            GalleryServer gallery;
            if (String.IsNullOrEmpty(Source)) {
                gallery = new GalleryServer();
            }
            else {
                gallery = new GalleryServer(Source);
            }

            Console.WriteLine(NuGetResources.PublishCommandPublishingPackage, _packageId, _packageVersion);
            gallery.PublishPackage(_apiKey, _packageId, _packageVersion);
            Console.WriteLine(NuGetResources.PublishCommandPackagePublished);
        }
    }
}
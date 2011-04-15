using System;
using System.ComponentModel.Composition;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "publish", "PublishCommandDescription",
        MinArgs = 2, MaxArgs = 3, UsageDescriptionResourceName = "PublishCommandUsageDescription",
        UsageSummaryResourceName = "PublishCommandUsageSummary")]
    public class PublishCommand : Command {

        

        [Option(typeof(NuGetResources), "PublishCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }


        public override void ExecuteCommand() {

            //Frist argument should be the package ID
            string packageId = Arguments[0];
            //Second argument should be the package Version
            string packageVersion = Arguments[1];
            //Third argument if present should be the API Key
            string userSetApiKey = null;
            if (Arguments.Count > 2) {
                userSetApiKey = Arguments[2];
            }

            //If the user passed a source use it for the gallery location
            string galleryServerUrl = String.IsNullOrEmpty(Source) ? GalleryServer.DefaultGalleryServerUrl : Source;
            var gallery = new GalleryServer(galleryServerUrl);

            //If the user did not pass an API Key look in the config file
            string apiKey = String.IsNullOrEmpty(userSetApiKey) ? CommandLineUtility.GetApiKey(Settings.UserSettings, galleryServerUrl) : userSetApiKey;

            Console.WriteLine(NuGetResources.PublishCommandPublishingPackage, packageId, packageVersion, CommandLineUtility.GetSourceDisplayName(Source));
            gallery.PublishPackage(apiKey, packageId, packageVersion);
            Console.WriteLine(NuGetResources.PublishCommandPackagePublished);
        }
    }
}
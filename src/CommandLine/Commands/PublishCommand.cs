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

        public IPackageSourceProvider SourceProvider { get; private set; }

        [ImportingConstructor]
        public PublishCommand(IPackageSourceProvider packageSourceProvider) {
            SourceProvider = packageSourceProvider;
        }

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
            string source = SourceProvider.ResolveAndValidateSource(Source) ?? GalleryServer.DefaultGalleryServerUrl;
            var gallery = new GalleryServer(source);

            //If the user did not pass an API Key look in the config file
            string apiKey = String.IsNullOrEmpty(userSetApiKey) ? CommandLineUtility.GetApiKey(SourceProvider, Settings.UserSettings, source) : userSetApiKey;

            Console.WriteLine(NuGetResources.PublishCommandPublishingPackage, packageId, packageVersion, SourceProvider.GetDisplayName(Source));
            gallery.PublishPackage(apiKey, packageId, packageVersion);
            Console.WriteLine(NuGetResources.PublishCommandPackagePublished);
        }
    }
}
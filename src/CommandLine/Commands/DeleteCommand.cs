using System;
using System.Globalization;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "delete", "DeleteCommandDescription",
        MinArgs = 2, MaxArgs = 3, UsageDescriptionResourceName = "DeleteCommandUsageDescription",
        UsageSummaryResourceName = "DeleteCommandUsageSummary")]
    public class DeleteCommand : Command {

        [Option(typeof(NuGetResources), "DeleteCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "DeleteCommandNoPromptDescription", AltName = "np")]
        public bool NoPrompt { get; set; }

        public override void ExecuteCommand() {

            //First argument should be the package ID
            string packageId = Arguments[0];
            //Second argument should be the package Version
            string packageVersion = Arguments[1];
            //Third argument, if present, should be the API Key
            string userSetApiKey = null;
            if (Arguments.Count > 2) {
                userSetApiKey = Arguments[2];
            }

            //If the user passed a source use it for the gallery location
            string galleryServerUrl;
            if (String.IsNullOrEmpty(Source)) {
                galleryServerUrl = GalleryServer.DefaultGalleryServerUrl;
            }
            else {
                galleryServerUrl = Source;
            }

            var gallery = new GalleryServer(galleryServerUrl);

            //If the user did not pass an API Key look in the config file
            string apiKey;
            if (String.IsNullOrEmpty(userSetApiKey)) {
                apiKey = CommandLineUtility.GetApiKey(Settings.UserSettings, galleryServerUrl);
            }
            else {
                apiKey = userSetApiKey;
            }


            if (NoPrompt || Console.Confirm(String.Format(CultureInfo.CurrentCulture, NuGetResources.DeleteCommandConfirm, packageId, packageVersion))) {
                Console.WriteLine(NuGetResources.DeleteCommandDeletingPackage, packageId, packageVersion);
                gallery.DeletePackage(apiKey, packageId, packageVersion);
                Console.WriteLine(NuGetResources.DeleteCommandDeletedPackage, packageId, packageVersion);
            }
            else {
                Console.WriteLine(NuGetResources.DeleteCommandCanceled);
            }

        }
    }
}
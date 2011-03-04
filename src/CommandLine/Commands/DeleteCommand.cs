using System;
using System.ComponentModel.Composition;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "delete", "DeleteCommandDescription",
        MinArgs = 3, MaxArgs = 3, UsageDescriptionResourceName = "DeleteCommandUsageDescription",
        UsageSummaryResourceName = "DeleteCommandUsageSummary")]
    public class DeleteCommand : Command {
        private string _apiKey;
        private string _packageId;
        private string _packageVersion;

        [Option(typeof(NuGetResources), "DeleteCommandSourceDescription")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "DeleteCommandNoPromptDescription")]
        public bool NoPrompt { get; set; }

        public override void ExecuteCommand() {
            //First argument should be the package ID
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

            if (NoPrompt || Console.Confirm(String.Format(NuGetResources.DeleteCommandConfirm, _packageId, _packageVersion))) {
                Console.WriteLine(NuGetResources.DeleteCommandDeletingPackage, _packageId, _packageVersion);
                gallery.DeletePackage(_apiKey, _packageId, _packageVersion);
                Console.WriteLine(NuGetResources.DeleteCommandDeletedPackage, _packageId, _packageVersion);
            }
            else {
                Console.WriteLine(NuGetResources.DeleteCommandCanceled);
            }

        }
    }
}
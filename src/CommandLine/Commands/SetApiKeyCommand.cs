using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "setApiKey", "SetApiKeyCommandDescription",
        MinArgs = 1, MaxArgs = 1, UsageDescriptionResourceName = "SetApiKeyCommandUsageDescription",
        UsageSummaryResourceName = "SetApiKeyCommandUsageSummary")]
    public class SetApiKeyCommand : Command {
        [Option(typeof(NuGetResources), "SetApiKeyCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }
        
        public override void ExecuteCommand() {
            //Frist argument should be the ApiKey
            string apiKey = Arguments[0];

            //If the user passed a source use it for the gallery location
            string galleryServerUrl;
            if (String.IsNullOrEmpty(Source)) {
                galleryServerUrl = GalleryServer.DefaultGalleryServerUrl;
            }
            else {
                galleryServerUrl = Source;
            }

            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            settings.SetEncryptedValue(CommandLineUtility.ApiKeysSectionName, galleryServerUrl, apiKey);

            Console.WriteLine(NuGetResources.SetApiKeyCommandApiKeySaved, apiKey, galleryServerUrl);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "setApiKey", "PushCommandDescription",
        MinArgs = 1, MaxArgs = 1, UsageDescriptionResourceName = "PushCommandUsageDescription",
        UsageSummaryResourceName = "PushCommandUsageSummary")]
    public class SetApiKeyCommand : Command {

        [Option("Source")]
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

            ISettings settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            settings.SetEncryptedValue("apiKeys", galleryServerUrl, apiKey);

            Console.WriteLine(NuGetResources.SetApiKeyCommandApiKeySaved, apiKey, galleryServerUrl);
        }
    }
}

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

            bool setSymbolServerKey = false;

            //If the user passed a source use it for the gallery location
            string galleryServerUrl;
            if (String.IsNullOrEmpty(Source)) {
                galleryServerUrl = GalleryServer.DefaultGalleryServerUrl;
                // If no source was specified, set the default symbol server key to be the same
                setSymbolServerKey = true;
            }
            else {
                CommandLineUtility.ValidateSource(Source);
                galleryServerUrl = Source;
            }

            var settings = Settings.UserSettings;
            settings.SetEncryptedValue(CommandLineUtility.ApiKeysSectionName, galleryServerUrl, apiKey);
            
            // Setup the symbol server key
            if (setSymbolServerKey) {
                settings.SetEncryptedValue(CommandLineUtility.ApiKeysSectionName, GalleryServer.DefaultSymbolServerUrl, apiKey);
                Console.WriteLine(NuGetResources.SetApiKeyCommandDefaultApiKeysSaved, 
                                  apiKey,
                                  CommandLineUtility.GetSourceDisplayName(galleryServerUrl),
                                  CommandLineUtility.GetSourceDisplayName(GalleryServer.DefaultSymbolServerUrl));
            }
            else {
                Console.WriteLine(NuGetResources.SetApiKeyCommandApiKeySaved, apiKey, CommandLineUtility.GetSourceDisplayName(galleryServerUrl));
            }
        }
    }
}
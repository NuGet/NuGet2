using System;
using System.ComponentModel.Composition;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "setApiKey", "SetApiKeyCommandDescription",
        MinArgs = 1, MaxArgs = 1, UsageDescriptionResourceName = "SetApiKeyCommandUsageDescription",
        UsageSummaryResourceName = "SetApiKeyCommandUsageSummary")]
    public class SetApiKeyCommand : Command {
        [Option(typeof(NuGetResources), "SetApiKeyCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        [ImportingConstructor]
        public SetApiKeyCommand(IPackageSourceProvider packageSourceProvider) {
            SourceProvider = packageSourceProvider;
        }

        public override void ExecuteCommand() {
            //Frist argument should be the ApiKey
            string apiKey = Arguments[0];

            bool setSymbolServerKey = false;

            //If the user passed a source use it for the gallery location
            string source;
            if (String.IsNullOrEmpty(Source)) {
                source = NuGetConstants.DefaultGalleryServerUrl;
                // If no source was specified, set the default symbol server key to be the same
                setSymbolServerKey = true;
            }
            else {
                source = SourceProvider.ResolveAndValidateSource(Source);
            }

            var settings = Settings.UserSettings;
            settings.SetEncryptedValue(CommandLineUtility.ApiKeysSectionName, source, apiKey);

            string sourceName = CommandLineUtility.GetSourceDisplayName(SourceProvider, source);

            // Setup the symbol server key
            if (setSymbolServerKey) {
                settings.SetEncryptedValue(CommandLineUtility.ApiKeysSectionName, NuGetConstants.DefaultSymbolServerUrl, apiKey);
                Console.WriteLine(NuGetResources.SetApiKeyCommandDefaultApiKeysSaved,
                                  apiKey,
                                  sourceName,
                                  CommandLineUtility.GetSourceDisplayName(SourceProvider, NuGetConstants.DefaultSymbolServerUrl));
            }
            else {
                Console.WriteLine(NuGetResources.SetApiKeyCommandApiKeySaved, apiKey, sourceName);
            }
        }
    }
}
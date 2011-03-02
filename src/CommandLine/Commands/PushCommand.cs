using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "push", "PushCommandDescription",
        MinArgs = 1, MaxArgs = 1, UsageDescriptionResourceName = "PushCommandUsageDescription",
        UsageSummaryResourceName = "PushCommandUsageSummary")]
    public class PushCommand : Command {

        [Option(typeof(NuGetResources), "PushCommandCreateOnlyDescription", AltName = "co")]
        public bool CreateOnly { get; set; }

        [Option(typeof(NuGetResources), "PushCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "PushCommandNoPersistApiKeyDescription")]
        public bool NoPersist { get; set; }

        [Option(typeof(NuGetResources), "ApiKeyDescription")]
        public string ApiKey { get; set; }
        
        public override void ExecuteCommand() {

            //Frist argument should be the package
            string packagePath = Arguments[0];

            //If the user passed a source use it for the gallery location
            GalleryServer gallery;
            if (String.IsNullOrEmpty(Source)) {
                gallery = new GalleryServer();
            }
            else {
                gallery = new GalleryServer(Source);
            }
            
            //If the user did not pass an API Key look in the config file
            string apiKey;
            ISettings settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            if(String.IsNullOrEmpty(ApiKey)){
                var value = settings.GetDecryptedValue("ApiKeys", gallery.Source);
                if (string.IsNullOrEmpty(value)) {
                    throw new CommandLineException(NuGetResources.NoApiKeyFound);
                }
                apiKey = value;

            }
            else {
                apiKey = ApiKey;
            }
            //Push the package to the server
            ZipPackage pkg = new ZipPackage(packagePath);
            using (Stream pkgStream = package.GetStream()) {
                gallery.CreatePackage(apiKey, pkgStream);
            }

            //Publish the package on the server
            if (!CreateOnly) {
                var cmd = new PublishCommand();
                cmd.Console = Console;
                cmd.Source = Source;
                cmd.Arguments = new List<string> { pkg.Id, pkg.Version.ToString(), apiKey };
                cmd.Execute();
            }

            //If we made it this far the push succeeded and we can try to save the API Key
            if (!NoPersist) {
                Console.WriteLine(NuGetResources.PushCommandSavingApiKey, apiKey, gallery.Source);
                try {
                    settings.SetEncryptedValue("ApiKeys", gallery.Source, apiKey);
                }
                catch {
                    Console.WriteError(NuGetResources.PushCommandUnableToSaveApiKey);
                }
            }

            else {
                var value = settings.GetDecryptedValue("ApiKeys", gallery.Source);
                if (!String.IsNullOrEmpty(value)) {
                    settings.DeleteValue("ApiKeys", gallery.Source);
                    Console.WriteLine(NuGetResources.PushCommandSavedApiKeyCleared);
                }
            }
        }
    }
}
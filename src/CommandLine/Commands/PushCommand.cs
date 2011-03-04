using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "push", "PushCommandDescription",
        MinArgs = 1, MaxArgs = 2, UsageDescriptionResourceName = "PushCommandUsageDescription",
        UsageSummaryResourceName = "PushCommandUsageSummary")]
    public class PushCommand : Command {

        [Option(typeof(NuGetResources), "PushCommandCreateOnlyDescription", AltName = "co")]
        public bool CreateOnly { get; set; }

        [Option(typeof(NuGetResources), "PushCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "PushCommandNoPersistApiKeyDescription")]
        public bool NoPersist { get; set; }
        
        public override void ExecuteCommand() {

            //Frist argument should be the package
            string packagePath = Arguments[0];

            //Second argument, if present, should be the API Key
            string userSetApiKey = null;
            if (Arguments.Count > 1) {
                userSetApiKey = Arguments[1];
            }

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
            if (String.IsNullOrEmpty(userSetApiKey)) {
                var value = settings.GetDecryptedValue("apiKeys", gallery.Source);
                if (string.IsNullOrEmpty(value)) {
                    throw new CommandLineException(NuGetResources.NoApiKeyFound);
                }
                apiKey = value;

            }
            else {
                apiKey = userSetApiKey;
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
            else {
                Console.WriteLine(NuGetResources.PushCommandPackageCreated);
            }

            // If the user passed no API Key and said to persist the key
            //  or if the use passed an API Key and said not to persist
            //  the key (temperary key use) then do nothing.
            
            // Save the API Key if the User passed a key and said to persist (the default)
            if (!String.IsNullOrEmpty(userSetApiKey) && !NoPersist) {
                Console.WriteLine(NuGetResources.PushCommandSavingApiKey, apiKey, gallery.Source);
                try {
                    settings.SetEncryptedValue("apiKeys", gallery.Source, apiKey);
                }
                catch {
                    Console.WriteError(NuGetResources.PushCommandUnableToSaveApiKey);
                }
            }

            // Delete the API Key if user did not pass a key and said not to persist the key
            if (String.IsNullOrEmpty(userSetApiKey) && NoPersist) {
                settings.DeleteValue("apiKeys", gallery.Source);
                Console.WriteLine(NuGetResources.PushCommandSavedApiKeyCleared);
            }

        }
    }
}
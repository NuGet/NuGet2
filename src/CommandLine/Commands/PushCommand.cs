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
        
        public override void ExecuteCommand() {

            //Frist argument should be the package
            string packagePath = Arguments[0];

            //Second argument, if present, should be the API Key
            string userSetApiKey = null;
            if (Arguments.Count > 1) {
                userSetApiKey = Arguments[1];
            }

            //If the user passed a source use it for the gallery location
            string galleryServerUrl;
            if (String.IsNullOrEmpty(Source)) {
                galleryServerUrl = GalleryServer.DefaultGalleryServerUrl;
            }
            else {
                galleryServerUrl = Source;
            }

            GalleryServer gallery = new GalleryServer(galleryServerUrl);
            
            //If the user did not pass an API Key look in the config file
            string apiKey;
            ISettings settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            if (String.IsNullOrEmpty(userSetApiKey)) {
                apiKey = CommandLineUtility.GetApiKey(settings, galleryServerUrl);
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

        }
    }
}
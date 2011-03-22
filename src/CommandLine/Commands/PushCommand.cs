using System;
using System.Collections.Generic;
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
            
            string galleryServerUrl = GetSource(packagePath);

            var gallery = new GalleryServer(galleryServerUrl);

            //If the user did not pass an API Key look in the config file
            string apiKey;
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            if (String.IsNullOrEmpty(userSetApiKey)) {
                apiKey = CommandLineUtility.GetApiKey(settings, galleryServerUrl);
            }
            else {
                apiKey = userSetApiKey;
            }

            //Push the package to the server
            var package = new ZipPackage(packagePath);
            using (Stream pkgStream = package.GetStream()) {
                gallery.CreatePackage(apiKey, pkgStream);
            }

            //Publish the package on the server
            if (!CreateOnly) {
                var cmd = new PublishCommand();
                cmd.Console = Console;
                cmd.Source = galleryServerUrl;
                cmd.Arguments = new List<string> { package.Id, package.Version.ToString(), apiKey };
                cmd.Execute();
            }
            else {
                Console.WriteLine(NuGetResources.PushCommandPackageCreated);
            }
        }

        private string GetSource(string packagePath) {
            //If the user passed a source use it for the gallery location
            string galleryServerUrl;
            if (String.IsNullOrEmpty(Source)) {
                galleryServerUrl = GetDefaultUrl(packagePath);
            }
            else {
                galleryServerUrl = Source;
            }

            return galleryServerUrl;
        }

        private string GetDefaultUrl(string packagePath) {
            if (packagePath.EndsWith(PackCommand.SymbolsExtension, StringComparison.OrdinalIgnoreCase)) {
                return GalleryServer.DefaultSymbolServerUrl;
            }
            return GalleryServer.DefaultGalleryServerUrl;
        }
    }
}
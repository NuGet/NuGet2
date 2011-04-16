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
            // Frist argument should be the package
            string packagePath = Arguments[0];

            // Don't push symbols by default
            bool pushSymbols = false;
            string source = null;

            if (!String.IsNullOrEmpty(Source)) {
                CommandLineUtility.ValidateSource(Source);
                source = Source;
            }
            else {
                if (packagePath.EndsWith(PackCommand.SymbolsExtension, StringComparison.OrdinalIgnoreCase)) {
                    source = GalleryServer.DefaultSymbolServerUrl;
                }
                else {
                    source = GalleryServer.DefaultGalleryServerUrl;
                    pushSymbols = true;
                }
            }

            PushPackage(packagePath, source);

            if (pushSymbols) {
                // Get the symbol package for this package
                string symbolPackagePath = GetSymbolsPath(packagePath);

                // Push the symbols package if it exists
                if (File.Exists(symbolPackagePath)) {
                    source = GalleryServer.DefaultSymbolServerUrl;

                    // See if the api key exists
                    string apiKey = GetApiKey(source, throwIfNotFound: false);

                    if (String.IsNullOrEmpty(apiKey)) {
                        Console.WriteWarning(NuGetResources.Warning_SymbolServerNotConfigured, Path.GetFileName(symbolPackagePath), NuGetResources.DefaultSymbolServer);
                    }
                    else {
                        PushPackage(symbolPackagePath, source, apiKey);
                    }
                }
            }
        }

        /// <summary>
        /// Get the symbols package from the original package. Removes the .nupkg and adds .symbols.nupkg
        /// </summary>
        private string GetSymbolsPath(string packagePath) {
            string symbolPath = Path.GetFileNameWithoutExtension(packagePath) + PackCommand.SymbolsExtension;
            string packageDir = Path.GetDirectoryName(packagePath);
            return Path.Combine(packageDir, symbolPath);
        }

        private void PushPackage(string packagePath, string source, string apiKey = null) {
            var gallery = new GalleryServer(source);

            // Use the specified api key or fall back to default behavior
            apiKey = apiKey ?? GetApiKey(source);

            // Push the package to the server
            var package = new ZipPackage(packagePath);

            Console.WriteLine(NuGetResources.PushCommandPushingPackage, package.GetFullName(), CommandLineUtility.GetSourceDisplayName(source));

            using (Stream stream = package.GetStream()) {
                gallery.CreatePackage(apiKey, stream);
            }

            // Publish the package on the server
            if (!CreateOnly) {
                var cmd = new PublishCommand();
                cmd.Console = Console;
                cmd.Source = source;
                cmd.Arguments = new List<string> { package.Id, package.Version.ToString(), apiKey };
                cmd.Execute();
            }
            else {
                Console.WriteLine(NuGetResources.PushCommandPackageCreated, source);
            }
        }

        private string GetApiKey(string source, bool throwIfNotFound = true) {
            string apiKey = null;

            // Second argument, if present, should be the API Key
            if (Arguments.Count > 1) {
                apiKey = Arguments[1];
            }

            // If the user did not pass an API Key look in the config file
            if (String.IsNullOrEmpty(apiKey)) {
                apiKey = CommandLineUtility.GetApiKey(Settings.UserSettings, source, throwIfNotFound);
            }

            return apiKey;
        }
    }
}
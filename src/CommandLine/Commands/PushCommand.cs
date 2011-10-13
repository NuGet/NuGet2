using System;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Common;

namespace NuGet.Commands
{
    [Command(typeof(NuGetResources), "push", "PushCommandDescription",
        MinArgs = 1, MaxArgs = 2, UsageDescriptionResourceName = "PushCommandUsageDescription",
        UsageSummaryResourceName = "PushCommandUsageSummary", UsageExampleResourceName = "PushCommandUsageExamples")]
    public class PushCommand : Command
    {
        [Option(typeof(NuGetResources), "PushCommandCreateOnlyDescription", AltName = "co")]
        public bool CreateOnly { get; set; }

        [Option(typeof(NuGetResources), "PushCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        public ISettings Settings { get; private set; }

        [ImportingConstructor]
        public PushCommand(IPackageSourceProvider packageSourceProvider, ISettings settings)
        {
            SourceProvider = packageSourceProvider;
            Settings = settings;
        }

        public override void ExecuteCommand()
        {
            // Frist argument should be the package
            string packagePath = Arguments[0];

            // Don't push symbols by default
            bool pushSymbols = false;
            string source = null;

            if (!String.IsNullOrEmpty(Source))
            {
                source = SourceProvider.ResolveAndValidateSource(Source);
            }
            else
            {
                if (packagePath.EndsWith(PackCommand.SymbolsExtension, StringComparison.OrdinalIgnoreCase))
                {
                    source = NuGetConstants.DefaultSymbolServerUrl;
                }
                else
                {
                    source = NuGetConstants.DefaultGalleryServerUrl;
                    pushSymbols = true;
                }
            }

            PushPackage(packagePath, source);

            if (pushSymbols)
            {
                PushSymbols(packagePath);
            }
        }

        private void PushSymbols(string packagePath)
        {
            // Get the symbol package for this package
            string symbolPackagePath = GetSymbolsPath(packagePath);

            // Push the symbols package if it exists
            if (File.Exists(symbolPackagePath))
            {
                string source = NuGetConstants.DefaultSymbolServerUrl;

                // See if the api key exists
                string apiKey = GetApiKey(source, throwIfNotFound: false);

                if (String.IsNullOrEmpty(apiKey))
                {
                    Console.WriteWarning(NuGetResources.Warning_SymbolServerNotConfigured, Path.GetFileName(symbolPackagePath), NuGetResources.DefaultSymbolServer);
                }
                else
                {
                    PushPackage(symbolPackagePath, source, apiKey);
                }
            }
        }

        /// <summary>
        /// Get the symbols package from the original package. Removes the .nupkg and adds .symbols.nupkg
        /// </summary>
        private static string GetSymbolsPath(string packagePath)
        {
            string symbolPath = Path.GetFileNameWithoutExtension(packagePath) + PackCommand.SymbolsExtension;
            string packageDir = Path.GetDirectoryName(packagePath);
            return Path.Combine(packageDir, symbolPath);
        }

        private void PushPackage(string packagePath, string source, string apiKey = null)
        {
            var packageServer = new PackageServer(source, CommandLineConstants.UserAgent);
            // Use the specified api key or fall back to default behavior
            apiKey = apiKey ?? GetApiKey(source);

            // Push the package to the server
            var package = new ZipPackage(packagePath);

            string sourceName = CommandLineUtility.GetSourceDisplayName(source);
            Console.WriteLine(NuGetResources.PushCommandPushingPackage, package.GetFullName(), sourceName);

            using (Stream stream = package.GetStream())
            {
                packageServer.CreatePackage(apiKey, stream);
            }

            // Publish the package on the server
            if (!CreateOnly)
            {
                var cmd = new PublishCommand(SourceProvider, Settings);
                cmd.Console = Console;
                cmd.Source = source;
                cmd.Arguments.AddRange(new[] { package.Id, package.Version.ToString(), apiKey });
                cmd.Execute();
            }
            else
            {
                Console.WriteLine(NuGetResources.PushCommandPackageCreated, source);
            }
        }

        private string GetApiKey(string source, bool throwIfNotFound = true)
        {
            string apiKey = null;

            // Second argument, if present, should be the API Key
            if (Arguments.Count > 1)
            {
                apiKey = Arguments[1];
            }

            // If the user did not pass an API Key look in the config file
            if (String.IsNullOrEmpty(apiKey))
            {
                apiKey = CommandLineUtility.GetApiKey(Settings, source, throwIfNotFound);
            }

            return apiKey;
        }
    }
}
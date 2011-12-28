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

        [Option(typeof(NuGetResources), "CommandApiKey")]
        public string ApiKey { get; set; }

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
            // First argument should be the package
            string packagePath = Arguments[0];

            // Don't push symbols by default
            string source = ResolveSource(packagePath);

            var apiKey = GetApiKey(source);
            if (String.IsNullOrEmpty(apiKey))
            {
                throw new CommandLineException(NuGetResources.NoApiKeyFound, CommandLineUtility.GetSourceDisplayName(source));
            }

            PushPackage(packagePath, source, apiKey);

            if (source.Equals(NuGetConstants.DefaultGalleryServerUrl, StringComparison.OrdinalIgnoreCase))
            {
                PushSymbols(packagePath);
            }
        }

        private string ResolveSource(string packagePath)
        {
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
                }
            }
            return source;
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

        private void PushPackage(string packagePath, string source, string apiKey)
        {
            var packageServer = new PackageServer(source, CommandLineConstants.UserAgent);

            string[] packagesToPush = Directory.GetFiles(Environment.CurrentDirectory, packagePath, SearchOption.TopDirectoryOnly);

            foreach (string packageToPush in packagesToPush)
            {
                if (string.Equals(Path.GetExtension(packageToPush), NuGet.Constants.PackageExtension, StringComparison.OrdinalIgnoreCase))
                {
                    // Push the package to the server
                    var package = new ZipPackage(packageToPush);

                    string sourceName = CommandLineUtility.GetSourceDisplayName(source);
                    Console.WriteLine(NuGetResources.PushCommandPushingPackage, package.GetFullName(), sourceName);

                    using (Stream stream = package.GetStream())
                    {
                        packageServer.PushPackage(apiKey, stream);
                    }

                    if (CreateOnly)
                    {
                        Console.WriteWarning(NuGetResources.Warning_PublishPackageDeprecated);
                    }
                    Console.WriteLine(NuGetResources.PushCommandPackagePushed);
                }
            }
        }

        private string GetApiKey(string source, bool throwIfNotFound = true)
        {
            if (!String.IsNullOrEmpty(ApiKey))
            {
                return ApiKey;
            }

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
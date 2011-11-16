using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.MSBuild.Resources;

namespace NuGet.MSBuild
{
    public class NuGetPush : Task
    {
        internal static readonly string SymbolsExtension = ".symbols" + Constants.PackageExtension;
        internal static readonly string ApiKeysSectionName = "apikeys";

        private readonly IPackageServerFactory _packageServerFactory;
        private readonly IPackageFactory _zipPackageFactory;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly ISettings _settings;

        [Required]
        public string PackagePath { get; set; }

        public bool CreateOnly { get; set; }

        public string Source { get; set; }

        public string ApiKey { get; set; }

        public NuGetPush()
            : this(new PackageServerFactory(), new ZipPackageFactory(), new FileSystemProvider(), Settings.DefaultSettings)
        {
        }

        public NuGetPush(IPackageServerFactory packageServerFactory, IPackageFactory zipPackageFactory, IFileSystemProvider fileSystemProvider, ISettings settings)
        {
            _packageServerFactory = packageServerFactory;
            _zipPackageFactory = zipPackageFactory;
            _fileSystemProvider = fileSystemProvider;
            _settings = settings;
        }

        public override bool Execute()
        {
            // Don't push symbols by default
            bool pushSymbols = false;
            string source = null;

            if (!String.IsNullOrEmpty(Source))
            {
                source = Source;
            }
            else
            {
                if (PackagePath.EndsWith(SymbolsExtension, StringComparison.OrdinalIgnoreCase))
                {
                    source = NuGetConstants.DefaultSymbolServerUrl;
                }
                else
                {
                    source = NuGetConstants.DefaultGalleryServerUrl;
                    pushSymbols = true;
                }
            }

            var fileSystem = _fileSystemProvider.CreateFileSystem(Path.GetDirectoryName(PackagePath));
            if (!fileSystem.FileExists(PackagePath))
            {
                Log.LogError(NuGetResources.PackageDoesNotExist, PackagePath);
                return false;
            }

            if (String.IsNullOrEmpty(ApiKey))
            {
                // If the user did not pass an API Key look in the config file
                ApiKey = _settings.GetDecryptedValue(ApiKeysSectionName, source);
                if (String.IsNullOrEmpty(ApiKey))
                {
                    Log.LogMessage(NuGetResources.BlankApiKey);
                }
            }

            PushPackage(fileSystem, PackagePath, source);

            if (pushSymbols)
            {
                PushSymbols(fileSystem, source);
            }
            return true;
        }

        private void PushSymbols(IFileSystem fileSystem, string source)
        {
            // Get the symbol package for this package
            string symbolPackagePath = GetSymbolsPath(PackagePath);

            // Push the symbols package if it exists
            if (fileSystem.FileExists(symbolPackagePath))
            {
                source = NuGetConstants.DefaultSymbolServerUrl;

                Log.LogMessage(NuGetResources.PushCommandPushingPackage, Path.GetFileNameWithoutExtension(symbolPackagePath), source);

                if (String.IsNullOrEmpty(ApiKey))
                {
                    Log.LogWarning(NuGetResources.SymbolServerNotConfigured, Path.GetFileName(symbolPackagePath), NuGetResources.DefaultSymbolServer);
                }
                else
                {
                    PushPackage(fileSystem, symbolPackagePath, source);
                }
            }
        }

        private void PushPackage(IFileSystem fileSystem, string packagePath, string source)
        {
            var packageServer = _packageServerFactory.CreateFrom(source);

            IPackage package = _zipPackageFactory.CreatePackage(() => fileSystem.OpenFile(packagePath));

            // Push the package to the server
            Log.LogMessage(NuGetResources.PushCommandPushingPackage, package.GetFullName(), source);
            using (Stream stream = package.GetStream())
            {
                packageServer.PushPackage(ApiKey, stream);
            }

            // Publish the package on the server
            Log.LogMessage(NuGetResources.PushCommandPackageCreated, source);
        }

        /// <summary>
        /// Get the symbols package from the original package. Removes the .nupkg and adds .symbols.nupkg
        /// </summary>
        private static string GetSymbolsPath(string packagePath)
        {
            string symbolPath = Path.GetFileNameWithoutExtension(packagePath) + SymbolsExtension;
            string packageDir = Path.GetDirectoryName(packagePath);
            return Path.Combine(packageDir, symbolPath);
        }
    }
}
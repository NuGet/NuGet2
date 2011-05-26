using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;
using NuGet.MSBuild.Resources;

namespace NuGet.MSBuild {
    public class NuGetPush : Task {
        private readonly IGalleryServerFactory _galleryServerFactory;
        private readonly IPackageFactory _zipPackageFactory;
        private readonly IFileSystem _fileSystem;
        private readonly ISettings _settings;

        internal static readonly string SymbolsExtension = ".symbols" + Constants.PackageExtension;

        public static readonly string ApiKeysSectionName = "apikeys";

        public bool CreateOnly { get; set; }

        public string Source { get; set; }

        [Required]
        public string PackagePath { get; set; }

        public string ApiKey { get; set; }

        public NuGetPush() : this(null, null, null, null) { }

        public NuGetPush(IGalleryServerFactory galleryServerFactory, IPackageFactory zipPackageFactory, IFileSystem fileSystemWrapper, ISettings settings) {
            _galleryServerFactory = galleryServerFactory ?? new GalleryServerFactory();
            _zipPackageFactory = zipPackageFactory ?? new ZipPackageFactory();
            _fileSystem = fileSystemWrapper ?? new FileSystemWrapper();
            _settings = settings ?? new UserSettings(_fileSystem);
        }

        public override bool Execute() {

            // Don't push symbols by default
            bool pushSymbols = false;
            string source = null;

            if (!String.IsNullOrEmpty(Source)) {
                source = Source;
            } 
            else {
                if (PackagePath.EndsWith(SymbolsExtension, StringComparison.OrdinalIgnoreCase)) {
                    source = GalleryServer.DefaultSymbolServerUrl;
                } 
                else {
                    source = GalleryServer.DefaultGalleryServerUrl;
                    pushSymbols = true;
                }
            }

            if (!_fileSystem.FileExists(PackagePath)) {
                Log.LogError(NuGetResources.PackageDoesNotExist, PackagePath);
                return false;
            }

            if (String.IsNullOrEmpty(ApiKey)) {
                // If the user did not pass an API Key look in the config file
                ApiKey = _settings.GetDecryptedValue(ApiKeysSectionName, source);
                if (String.IsNullOrEmpty(ApiKey))
                    Log.LogMessage(NuGetResources.BlankApiKey);

            }

            PushPackage(PackagePath, source);

            if (pushSymbols) {
                // Get the symbol package for this package
                string symbolPackagePath = GetSymbolsPath(PackagePath);

                // Push the symbols package if it exists
                if (_fileSystem.FileExists(symbolPackagePath)) {
                    source = GalleryServer.DefaultSymbolServerUrl;

                    if (String.IsNullOrEmpty(ApiKey)) {
                        Log.LogWarning(NuGetResources.SymbolServerNotConfigured, Path.GetFileName(symbolPackagePath), NuGetResources.DefaultSymbolServer);
                    } 
                    else {
                        PushPackage(symbolPackagePath, source);
                    }
                }
            }
            return true;
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

        private void PushPackage(string packagePath, string source)
        {
            IGalleryServer gallery = _galleryServerFactory.createFrom(source);

            // Push the package to the server
            IPackage package = _zipPackageFactory.CreatePackage(packagePath);

            Log.LogMessage(NuGetResources.PushCommandPushingPackage, package.GetFullName(), source);

            using (Stream stream = package.GetStream()) {
                gallery.CreatePackage(ApiKey, stream);
            }

            // Publish the package on the server
            if (!CreateOnly) {
                gallery.PublishPackage(ApiKey, package.Id, package.Version.ToString());
                Log.LogMessage(NuGetResources.PushCommandPackagePublished, source);
            } 
            else {
                Log.LogMessage(NuGetResources.PushCommandPackageCreated, source);
            }
        }
    }
}
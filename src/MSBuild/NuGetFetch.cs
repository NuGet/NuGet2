using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.MSBuild.Resources;

namespace NuGet.MSBuild {
    public class NuGetFetch : Task {
        private readonly IPackageManagerFactory _packageManagerFactory;
        private IFileSystem _fileSystem;
        private readonly IPackageReferenceProvider _packageReferenceProvider;
        private readonly IAggregateRepositoryFactory _aggregateRepositoryFactory;

        [Required]
        public string PackageDir { get; set; }

        [Required]
        public string PackageConfigFile { get; set; }

        public string NugetConfigPath { get; set; }

        public bool ExcludeVersion { get; set; }

        public bool IgnoreUnavailableRepositories { get; set; }

        public string[] FeedUrls { get; set; }

        public NuGetFetch()
            : this(null, null, null, null)
        { }

        public NuGetFetch(
            IPackageManagerFactory packageManagerFactory,
            IFileSystem fileSystem,
            IPackageReferenceProvider packageReferenceProvider,
            IAggregateRepositoryFactory aggregateRepositoryFactory) {
            _packageManagerFactory = packageManagerFactory ?? new PackageManagerFactory();
            _fileSystem = fileSystem;
            _packageReferenceProvider = packageReferenceProvider ?? new PackageReferenceProvider();
            _aggregateRepositoryFactory = aggregateRepositoryFactory ?? new AggregateRepositoryFactory();
        }

        public override bool Execute() {
            bool installedAny = false;

            IPackageRepository packageRepository;
            Func<IPackageRepository> creator;
            if (FeedUrls == null || !FeedUrls.Any()) {
                if (!String.IsNullOrWhiteSpace(NugetConfigPath)) {
                    Log.LogMessage(MessageImportance.Normal, NuGetResources.BuildSpecificConfig);
                    creator = (() => _aggregateRepositoryFactory.createSpecificSettingsRepository(NugetConfigPath));
                }
                else {
                    Log.LogMessage(MessageImportance.Normal, NuGetResources.MachineSpecificConfig);
                    creator = (() => _aggregateRepositoryFactory.createDefaultSettingsRepository());
                }
            } 
            else {
                if (!String.IsNullOrWhiteSpace(NugetConfigPath)) {
                    Log.LogError(NuGetResources.FeedsAndConfigSpecified);
                    return false;
                }
                Log.LogMessage(MessageImportance.Normal, NuGetResources.BuildSpecificFeeds);
                creator = (()=>_aggregateRepositoryFactory.createSpecificFeedsRepository(IgnoreUnavailableRepositories, FeedUrls));
            }
            
            Log.LogMessage(NuGetResources.LookingForDependencies);
           
            try {
                packageRepository = creator();
            }
            catch (InvalidOperationException ex) {
                Log.LogError(ex.Message);
                return false;
            }

            _fileSystem = _fileSystem ?? new PhysicalFileSystem(PackageDir);
            IPackageManager packageManager = _packageManagerFactory.CreateFrom(packageRepository, PackageDir);
            packageManager.Logger = new MSBuildLogger(Log);

            IEnumerable<PackageReference> packageReferences;
            try {
                packageReferences =_packageReferenceProvider.getPackageReferences(PackageConfigFile);
            }
            catch (FileNotFoundException) {
                Log.LogError(NuGetResources.PackageConfigNotFound, PackageConfigFile);
                return false;
            }
            catch (InvalidOperationException) {
                Log.LogError(NuGetResources.PackageConfigParseError, PackageConfigFile);
                return false;
            }

            Log.LogMessage(NuGetResources.StartingFetch);
            foreach (var package in packageReferences) {
                if (!IsPackageInstalled(package.Id, package.Version, packageManager)) {
                    // Note that we ignore dependencies here because packages.config already contains the full closure
                    packageManager.InstallPackage(package.Id, package.Version, ignoreDependencies: true);
                    installedAny = true;
                }
            }

            if (!installedAny) {
                Log.LogMessage(NuGetResources.NoPackagesFound);
            }
            return true;
        }

        // Do a very quick check of whether a package in installed by checked whether the nupkg file exists
        private bool IsPackageInstalled(string packageId, Version version, IPackageManager packageManager) {
            var packageDir = packageManager.PathResolver.GetPackageDirectory(packageId, version);
            var packageFile = packageManager.PathResolver.GetPackageFileName(packageId, version);

            string packagePath = Path.Combine(packageDir, packageFile);

            return _fileSystem.FileExists(packagePath);
        }
    }
}


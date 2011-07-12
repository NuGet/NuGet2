using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "install", "InstallCommandDescription",
        MinArgs = 1, MaxArgs = 1, UsageSummaryResourceName = "InstallCommandUsageSummary", 
        UsageDescriptionResourceName = "InstallCommandUsageDescription", 
        UsageExampleResourceName = "InstallCommandUsageExamples")]
    public class InstallCommand : Command {
        private readonly List<string> _sources = new List<string>();

        [Option(typeof(NuGetResources), "InstallCommandSourceDescription")]
        public ICollection<string> Source {
            get { return _sources; }
        }

        [Option(typeof(NuGetResources), "InstallCommandOutputDirDescription")]
        public string OutputDirectory { get; set; }

        [Option(typeof(NuGetResources), "InstallCommandVersionDescription")]
        public string Version { get; set; }

        [Option(typeof(NuGetResources), "InstallCommandExcludeVersionDescription", AltName = "x")]
        public bool ExcludeVersion { get; set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        private bool AllowMultipleVersions {
            get { return !ExcludeVersion; }
        }

        [ImportingConstructor]
        public InstallCommand(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider) {
            if (packageRepositoryFactory == null) {
                throw new ArgumentNullException("packageRepositoryFactory");
            }

            if (sourceProvider == null) {
                throw new ArgumentNullException("sourceProvider");
            }

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand() {
            IFileSystem fileSystem = CreateFileSystem();

            // If the first argument is a packages.config file, install everything it lists
            // Otherwise, treat the first argument as a package Id
            if (Path.GetFileName(Arguments[0]).Equals(PackageReferenceRepository.PackageReferenceFile, StringComparison.OrdinalIgnoreCase)) {
                InstallPackagesFromConfigFile(fileSystem, GetPackageReferenceFile(Arguments[0]));
            }
            else {
                PackageManager packageManager = CreatePackageManager(fileSystem);
                string packageId = Arguments[0];
                Version version = Version != null ? new Version(Version) : null;

                bool result = InstallPackage(packageManager, fileSystem, packageId, version);
                if (!result) {
                    Console.WriteLine(NuGetResources.InstallCommandPackageAlreadyExists, packageId);
                }
            }
        }

        protected virtual PackageReferenceFile GetPackageReferenceFile(string path) {
            return new PackageReferenceFile(Path.GetFullPath(path));
        }

        private IPackageRepository GetRepository() {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }

        private void InstallPackagesFromConfigFile(IFileSystem fileSystem, PackageReferenceFile file) {
            var packageReferences = file.GetPackageReferences().ToList();
            PackageManager packageManager = CreatePackageManager(fileSystem, useMachineCache: true);

            bool installedAny = false;
            foreach (var package in packageReferences) {
                // Note that we ignore dependencies here because packages.config already contains the full closure
                installedAny |= InstallPackage(packageManager, fileSystem, package.Id, package.Version);
            }

            if (!installedAny && packageReferences.Any()) {
                Console.WriteLine(NuGetResources.InstallCommandNothingToInstall, PackageReferenceRepository.PackageReferenceFile);
            }
        }

        private bool InstallPackage(PackageManager packageManager, IFileSystem fileSystem, string packageId, Version version) {
            if (AllowMultipleVersions && IsPackageInstalled(packageId, version, packageManager, fileSystem)) {
                // Use a fast check to verify if the package is already installed. We'll do this by checking if the package directory exists on disk.
                return false;
            }
            else if (!AllowMultipleVersions) {
                var installedPackage = packageManager.LocalRepository.FindPackage(packageId);
                if (installedPackage != null) {
                    if (version != null && installedPackage.Version >= version) {
                        // If the package is already installed (or the version being installed is lower), then we do not need to do anything. 
                        return false;
                    }
                    else if (packageManager.SourceRepository.Exists(packageId, version)) {
                        // If the package is already installed, but
                        // (a) the version we require is different from the one that is installed, 
                        // (b) side-by-side is disabled
                        // we need to uninstall it.
                        // However, before uninstalling, make sure the package exists in the source repository. 
                        packageManager.UninstallPackage(installedPackage, forceRemove: false, removeDependencies: true);
                    }
                }
            }
            packageManager.InstallPackage(packageId, version);
            return true;
        }

        protected virtual PackageManager CreatePackageManager(IFileSystem fileSystem, bool useMachineCache = false) {
            var repository = GetRepository();

            if (useMachineCache) {
                repository = new AggregateRepository(new[] { MachineCache.Default, repository });
            }

            var pathResolver = new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: AllowMultipleVersions);
            var packageManager = new PackageManager(repository, pathResolver, fileSystem);
            packageManager.Logger = Console;

            return packageManager;
        }

        protected virtual IFileSystem CreateFileSystem() {
            // Use the passed in install path if any, and default to the current dir
            string installPath = OutputDirectory ?? Directory.GetCurrentDirectory();

            return new PhysicalFileSystem(installPath);
        }

        // Do a very quick check of whether a package in installed by checked whether the nupkg file exists
        private static bool IsPackageInstalled(string packageId, Version version, PackageManager packageManager, IFileSystem fileSystem) {
            var packageDir = packageManager.PathResolver.GetPackageDirectory(packageId, version);
            var packageFile = packageManager.PathResolver.GetPackageFileName(packageId, version);

            string packagePath = Path.Combine(packageDir, packageFile);

            return fileSystem.FileExists(packagePath);
        }
    }
}

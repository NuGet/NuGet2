using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "install", "InstallCommandDescription",
        MinArgs = 1, MaxArgs = 1,
        UsageSummaryResourceName = "InstallCommandUsageSummary", UsageDescriptionResourceName = "InstallCommandUsageDescription")]
    public class InstallCommand : Command {
        private readonly List<string> _sources = new List<string>();

        [Option(typeof(NuGetResources), "InstallCommandSourceDescription")]
        public List<string> Source {
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
            IFileSystem fileSystem = GetFileSystem();

            // If the first argument is a packages.config file, install everything it lists
            // Otherwise, treat the first argument as a package Id
            if (Path.GetFileName(Arguments[0]).Equals(PackageReferenceRepository.PackageReferenceFile, StringComparison.OrdinalIgnoreCase)) {
                InstallPackagesFromConfigFile(fileSystem, GetPackageReferenceFile(Arguments[0]));
            }
            else {
                PackageManager packageManager = GetPackageManager(fileSystem);

                string packageId = Arguments[0];

                Version version = Version != null ? new Version(Version) : null;

                if (!AllowMultipleVersions) {
                    // If side-by-side is turned off, we need to try and update the package.
                    var installedPackage = packageManager.LocalRepository.FindPackage(packageId);
                    var sourcePackage = packageManager.SourceRepository.FindPackage(packageId, version);
                    if (installedPackage != null && sourcePackage != null && installedPackage.Version != sourcePackage.Version) {
                        packageManager.UninstallPackage(installedPackage);
                    }
                }

                packageManager.InstallPackage(packageId, version);
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
            PackageManager packageManager = GetPackageManager(fileSystem, useMachineCache: true);

            bool installedAny = false;
            foreach (var package in packageReferences) {
                if (!IsPackageInstalled(package.Id, package.Version, packageManager, fileSystem)) {
                    // Note that we ignore dependencies here because packages.config already contains the full closure
                    packageManager.InstallPackage(package.Id, package.Version, ignoreDependencies: true);
                    installedAny = true;
                }
            }

            if (!installedAny && packageReferences.Any()) {
                Console.WriteLine(NuGetResources.InstallCommandNothingToInstall, PackageReferenceRepository.PackageReferenceFile);
            }
        }

        protected virtual PackageManager GetPackageManager(IFileSystem fileSystem, bool useMachineCache = false) {
            var repository = GetRepository();

            if (useMachineCache) {
                repository = new AggregateRepository(new[] { MachineCache.Default, repository });
            }

            var pathResolver = new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: AllowMultipleVersions);
            var packageManager = new PackageManager(repository, pathResolver, fileSystem);
            packageManager.Logger = Console;

            return packageManager;
        }

        protected virtual IFileSystem GetFileSystem() {
            // Use the passed in install path if any, and default to the current dir
            string installPath = OutputDirectory ?? Directory.GetCurrentDirectory();

            return new PhysicalFileSystem(installPath);
        }

        // Do a very quick check of whether a package in installed by checked whether the nupkg file exists
        private bool IsPackageInstalled(string packageId, Version version, PackageManager packageManager, IFileSystem fileSystem) {
            var packageDir = packageManager.PathResolver.GetPackageDirectory(packageId, version);
            var packageFile = packageManager.PathResolver.GetPackageFileName(packageId, version);

            string packagePath = Path.Combine(packageDir, packageFile);

            return fileSystem.FileExists(packagePath);
        }
    }
}

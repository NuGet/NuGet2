using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.MSBuild.Resources;

namespace NuGet.MSBuild
{
    public class NuGetFetch : Task
    {
        private readonly IPackageManagerFactory _packageManagerFactory;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _sourceProvider;

        [Required]
        public string PackageDir { get; set; }

        public string PackageConfigFile { get; set; }

        public string[] Sources { get; set; }

        public NuGetFetch()
            : this(new PackageManagerFactory(),
                   new FileSystemProvider(),
                   PackageRepositoryFactory.Default,
                   new PackageSourceProvider(Settings.DefaultSettings))
        {
        }

        public NuGetFetch(IPackageManagerFactory packageManagerFactory,
                          IFileSystemProvider fileSystemProvider,
                          IPackageRepositoryFactory repositoryFactory,
                          IPackageSourceProvider packageSourceProvider)
        {

            _packageManagerFactory = packageManagerFactory;
            _fileSystemProvider = fileSystemProvider;
            _repositoryFactory = repositoryFactory;
            _sourceProvider = packageSourceProvider;
        }

        public override bool Execute()
        {
            IPackageRepository sourceRepository;
            if (!TryCreateRepository(out sourceRepository))
            {
                // We were unable to determine a repository. Terminate here because there must have been an error.
                return false;
            }

            Log.LogMessage(NuGetResources.LookingForDependencies);

            var fileSystem = _fileSystemProvider.CreateFileSystem(PackageDir);
            var packageManager = _packageManagerFactory.CreateFrom(sourceRepository, PackageDir);
            packageManager.Logger = new MSBuildLogger(Log);

            IEnumerable<PackageReference> packageReferences;
            if (!TryGetPackageReferences(fileSystem, out packageReferences))
            {
                return false;
            }

            Log.LogMessage(NuGetResources.StartingFetch);

            bool installedAny;
            try
            {
                installedAny = InstallPackages(packageManager, fileSystem, packageReferences);
            }
            catch (InvalidOperationException ex)
            {
                Log.LogError(ex.Message);
                return false;
            }

            if (!installedAny)
            {
                Log.LogMessage(NuGetResources.NoPackagesFound);
            }

            return true;
        }

        private bool InstallPackages(IPackageManager packageManager, IFileSystem fileSystem, IEnumerable<PackageReference> packageReferences)
        {
            bool installedAny = false;
            foreach (var package in packageReferences)
            {
                if (!IsPackageInstalled(packageManager, fileSystem, package))
                {
                    // Note that we ignore dependencies here because packages.config already contains the full closure
                    packageManager.InstallPackage(package.Id, package.Version, ignoreDependencies: true, allowPrereleaseVersions: true);
                    installedAny = true;
                }
            }
            return installedAny;
        }

        private bool TryCreateRepository(out IPackageRepository repository)
        {
            var feedsSpecified = Sources != null && Sources.Any();
            // If the user specifies a source, use that. If not fall back to the default package sources.
            var sources = feedsSpecified ? Sources : _sourceProvider.GetEnabledPackageSources().Select(s => s.Source);

            Log.LogMessage(MessageImportance.Normal, NuGetResources.BuildSpecificFeeds);
            try
            {
                // For a user specified list of sources, throw exceptions letting them know 
                AggregateRepository aggregate = new AggregateRepository(_repositoryFactory, sources, ignoreFailingRepositories: !feedsSpecified);
                repository = new AggregateRepository(new[] { MachineCache.Default }.Concat(aggregate.Repositories));
                return true;
            }
            catch (InvalidOperationException exception)
            {
                Log.LogError(exception.Message);
            }
            catch (UriFormatException exception)
            {
                Log.LogError(exception.Message);
            }
            repository = null;
            return false;
        }

        private bool TryGetPackageReferences(IFileSystem fileSystem, out IEnumerable<PackageReference> packageReferences)
        {
            packageReferences = null;
            var packageConfigFile = String.IsNullOrEmpty(PackageConfigFile) ? PackageReferenceRepository.PackageReferenceFile : PackageConfigFile;

            if (!fileSystem.FileExists(packageConfigFile))
            {
                Log.LogError(NuGetResources.PackageConfigNotFound, packageConfigFile);
                return false;
            }

            try
            {
                var packageReferenceFile = new PackageReferenceFile(fileSystem, packageConfigFile);
                packageReferences = packageReferenceFile.GetPackageReferences().ToList();
                return true;
            }
            catch (InvalidOperationException)
            {
                Log.LogError(NuGetResources.PackageConfigParseError, packageConfigFile);
            }
            return false;
        }

        // Do a very quick check of whether a package in installed by checked whether the nupkg file exists
        private bool IsPackageInstalled(IPackageManager packageManager, IFileSystem fileSystem, PackageReference packageReference)
        {
            var packageDir = packageManager.PathResolver.GetPackageDirectory(packageReference.Id, packageReference.Version);
            var packageFile = packageManager.PathResolver.GetPackageFileName(packageReference.Id, packageReference.Version);

            string packagePath = Path.Combine(packageDir, packageFile);

            return fileSystem.FileExists(packagePath);
        }
    }
}


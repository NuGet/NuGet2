using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using EnvDTE;
using NuGetConsole;
using System.Linq;

namespace NuGet.VisualStudio
{
    [Export(typeof(IVsPackageRestorer))]
    public class VsPackageRestorer : IVsPackageRestorer
    {
        [ImportingConstructor]
        public VsPackageRestorer()
        {
        }

        public bool IsUserConsentGranted()
        {
            var settings = ServiceLocator.GetInstance<ISettings>();
            var packageRestoreConsent = new PackageRestoreConsent(settings);
            return packageRestoreConsent.IsGranted;
        }

        /// <summary>
        /// Returns true if the package is already installed in the local repository.
        /// </summary>
        /// <param name="fileSystem">The file system of the local repository.</param>
        /// <param name="packageId">The package id.</param>
        /// <param name="version">The package version</param>
        /// <returns>True if the package is installed in the local repository.</returns>
        private static bool IsPackageInstalled(IFileSystem fileSystem, string packageId, SemanticVersion version)
        {
            if (version != null)
            {
                // If we know exactly what package to lookup, check if it's already installed locally. 
                // We'll do this by checking if the package directory exists on disk.
                var localRepository = new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem);
                var packagePaths = localRepository.GetPackageLookupPaths(packageId, version);
                return packagePaths.Any(fileSystem.FileExists);
            }
            return false;
        }

        public void RestorePackages(Project project)
        {
            string packageReferenceFileFullPath;
            Tuple<string, string> packageReferenceFiles = VsUtility.GetPackageReferenceFileFullPaths(project);
            if (File.Exists(packageReferenceFiles.Item1))
            {
                packageReferenceFileFullPath = packageReferenceFiles.Item1;
            }
            else if (File.Exists(packageReferenceFiles.Item2))
            {
                packageReferenceFileFullPath = packageReferenceFiles.Item2;
            }
            else
            {
                return;
            }

            var packageReferenceFile = new PackageReferenceFile(packageReferenceFileFullPath);
            var packages = packageReferenceFile.GetPackageReferences().ToList();
            if (packages.Count == 0)
            {
                return;
            }

            var repoSettings = ServiceLocator.GetInstance<IRepositorySettings>();
            var fileSystem = new PhysicalFileSystem(repoSettings.RepositoryPath);
            var activePackageSourceRepository = ServiceLocator.GetInstance<IPackageRepository>();
            var repository = new AggregateRepository(new[] { MachineCache.Default, activePackageSourceRepository });
            IVsPackageManagerFactory packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            var packageManager = packageManagerFactory.CreatePackageManager(repository, useFallbackForDependencies: false);

            foreach (var package in packages)
            {
                if (IsPackageInstalled(fileSystem, package.Id, package.Version))
                {
                    continue;
                }

                using (packageManager.SourceRepository.StartOperation(RepositoryOperationNames.Restore, package.Id, package.Version.ToString()))
                {
                    var resolvedPackage = PackageHelper.ResolvePackage(
                        packageManager.SourceRepository, package.Id, package.Version);
                    NuGet.Common.PackageExtractor.InstallPackage(packageManager, resolvedPackage);
                }
            }
        }
    }
}

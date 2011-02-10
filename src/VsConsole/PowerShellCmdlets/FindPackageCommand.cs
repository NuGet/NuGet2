using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.PowerShell", "PS1101:AllCmdletsShouldAcceptPipelineInput", Justification = "Will investiage this one.")]
    [Cmdlet(VerbsCommon.Find, "Package", DefaultParameterSetName = "Default")]
    [OutputType(typeof(IPackage))]
    public class FindPackageCommand : GetPackageCommand {
        private const int MaxReturnedPackages = 30;

        public FindPackageCommand()
            : this(ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IPackageSourceProvider>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IRecentPackageRepository>()) {
        }

        public FindPackageCommand(IPackageRepositoryFactory repositoryFactory,
                          IPackageSourceProvider packageSourceProvider,
                          ISolutionManager solutionManager,
                          IVsPackageManagerFactory packageManagerFactory,
                          IPackageRepository recentPackagesRepository)
            : base(repositoryFactory, packageSourceProvider, solutionManager, packageManagerFactory, recentPackagesRepository) {
        }

        protected override void ProcessRecordCore() {
            // Since this is used for intellisense, we need to limit the number of packages that we return. Otherwise,
            // typing InstallPackage TAB would download the entire feed.
            First = MaxReturnedPackages;
            base.ProcessRecordCore();
        }

        protected override IEnumerable<IPackage> FilterPackages(IPackageRepository sourceRepository) {
            var packages = sourceRepository.GetPackages();
            if (!String.IsNullOrEmpty(Filter)) {
                packages = packages.Where(p => p.Id.ToLower().StartsWith(Filter.ToLower()));
            }

            return packages.OrderByDescending(p => p.DownloadCount)
                           .ThenBy(p => p.Id)
                           .DistinctLast(PackageEqualityComparer.Id, PackageComparer.Version);
        }

        protected override IEnumerable<IPackage> FilterPackagesForUpdate(IPackageRepository sourceRepository) {
            IPackageRepository localRepository = PackageManager.LocalRepository;
            var packagesToUpdate = localRepository.GetPackages();

            if (!String.IsNullOrEmpty(Filter)) {
                packagesToUpdate = packagesToUpdate.Where(p => p.Id.ToLower().StartsWith(Filter.ToLower()));
            }

            return sourceRepository.GetUpdates(packagesToUpdate)
                                   .OrderByDescending(p => p.DownloadCount)
                                   .ThenBy(p => p.Id)
                                   .DistinctLast(PackageEqualityComparer.Id, PackageComparer.Version);
        }

        protected override void Log(MessageLevel level, string formattedMessage) {
            // We don't want this cmdlet to print anything
        }
    }
}

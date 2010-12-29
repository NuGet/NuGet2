using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;

namespace NuGet.VisualStudio.Cmdlets {
    [Cmdlet(VerbsCommon.Find, "Package", DefaultParameterSetName = "Default")]
    public class FindPackageCmdlet : GetPackageCmdlet {

        private const int MaxReturnedPackages = 30;

        public FindPackageCmdlet()
            : this(ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IPackageSourceProvider>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>()) {
        }

        public FindPackageCmdlet(IPackageRepositoryFactory repositoryFactory,
                          IPackageSourceProvider packageSourceProvider,
                          ISolutionManager solutionManager,
                          IVsPackageManagerFactory packageManagerFactory)
            : base(repositoryFactory, packageSourceProvider, solutionManager, packageManagerFactory) {
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

            packages = packages.OrderBy(p => p.Id);
            
            return packages;
        }

        protected override IEnumerable<IPackage> FilterPackagesForUpdate(IPackageRepository sourceRepository) {
            IPackageRepository localRepository = PackageManager.LocalRepository;
            var packagesToUpdate = localRepository.GetPackages();
            if (!String.IsNullOrEmpty(Filter)) {
                packagesToUpdate = packagesToUpdate.Where(p => p.Id.ToLower().StartsWith(Filter.ToLower()));
            }
            return sourceRepository.GetUpdates(packagesToUpdate);
        }

        protected override void Log(MessageLevel level, string formattedMessage) {
            // We don't want this cmdlet to print anything
        }
    }
}

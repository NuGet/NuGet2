using System.Collections.Generic;
using System.Windows;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class SolutionOnlineProvider : OnlineProvider {
        public SolutionOnlineProvider(
            IPackageRepository localRepository,
            ResourceDictionary resources,
            IPackageRepositoryFactory packageRepositoryFactory,
            IPackageSourceProvider packageSourceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager) :
            base(null, 
                localRepository, 
                resources, 
                packageRepositoryFactory, 
                packageSourceProvider, 
                packageManagerFactory, 
                providerServices, 
                progressProvider,
                solutionManager) {
        }

        protected override bool ExecuteAfterLicenseAggrement(
            PackageItem item, 
            IVsPackageManager activePackageManager, 
            IList<PackageOperation> operations) {
            return InstallPackageIntoSolution(item, activePackageManager, operations);
        }
    }
}
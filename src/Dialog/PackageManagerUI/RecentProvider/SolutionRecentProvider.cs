using System;
using System.Collections.Generic;
using System.Windows;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class SolutionRecentProvider : RecentProvider {
        public SolutionRecentProvider(
            IPackageRepository localRepository,
            ResourceDictionary resources,
            IPackageRepositoryFactory packageRepositoryFactory,
            IVsPackageManagerFactory packageManagerFactory,
            IPackageRepository recentPackagesRepository,
            IPackageSourceProvider packageSourceProvider,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager) : 
            base(
                null, 
                localRepository, 
                resources, 
                packageRepositoryFactory, 
                packageManagerFactory,
                recentPackagesRepository,
                packageSourceProvider,
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
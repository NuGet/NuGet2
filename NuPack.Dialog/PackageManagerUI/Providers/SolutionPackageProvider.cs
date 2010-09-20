using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.PackageManagerUI.Providers {
    class SolutionPackageProvider : PackageProvider {
        public SolutionPackageProvider(ResourceDictionary resources, IVsProgressPane progressPane)
            : base(resources, progressPane, "Solution", "All") {
            // Start the population
            PopulatePackages(null);
        }

        /// <summary>
        /// Gather all the packages
        /// </summary>
        void PopulatePackages(object data) {
            foreach (IPackage package in PackageManager.SolutionRepository.GetPackages()) {
                PackageRecords.Add(package);
            }
        }
    }
}

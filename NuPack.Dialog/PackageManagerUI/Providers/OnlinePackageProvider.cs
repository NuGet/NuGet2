using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.PackageManagerUI.Providers {
    internal class OnlinePackageProvider : PackageProvider {
        /// <summary>
        /// Construct the type library reference provider
        /// </summary>
        /// <param name="resources"></param>
        public OnlinePackageProvider(ResourceDictionary resources, IVsProgressPane progressPane)
            : base(resources, progressPane, "Packages", "All") {            
            // Start the population
            //ThreadPool.QueueUserWorkItem(new WaitCallback(PopulatePackages));
            PopulatePackages(null);
        }
        
        /// <summary>
        /// Gather all the packages
        /// </summary>
        void PopulatePackages(object data) {            
            IQueryable<IPackage> query = PackageManager.SourceRepository.GetPackages();
            foreach (IPackage package in query.Take(10)) {
                PackageRecords.Add(package);
            }
        }
       
        public void Install(string id) {
            ProjectManager.AddPackageReference(id);
        }
    }
}

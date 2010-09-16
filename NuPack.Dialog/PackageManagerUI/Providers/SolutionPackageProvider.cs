using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuPack.Dialog.ToolsOptionsUI;

namespace NuPack.Dialog.PackageManagerUI.Providers
{
    class SolutionPackageProvider : PackageProvider
    {
        public SolutionPackageProvider(ResourceDictionary resources, IVsProgressPane progressPane)
            : base(resources, progressPane, "Solution", "All")
        {
            // Start the population
            PopulatePackages(null);
        }

        /// <summary>
        /// Gather all the packages
        /// </summary>
        void PopulatePackages(object data)
        {
            string feed = Settings.RepositoryServiceUri;
            if (!String.IsNullOrEmpty(feed)) {
                EnvDTE.DTE dte = Utilities.ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

                NuPack.VisualStudio.VSPackageManager vsPackageManager = NuPack.VisualStudio.VSPackageManager.GetPackageManager(feed, dte);
                foreach (NuPack.Package package in vsPackageManager.SolutionRepository.GetPackages()) {
                    PackageRecords.Add(package);
                }
            }
        }
    }
}

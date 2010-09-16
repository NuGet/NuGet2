using System;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.PackageManagerUI.Providers {
    internal class OnlinePackageProvider : PackageProvider {
        private NuPack.VisualStudio.VSPackageManager _vsPackageManager;
        private EnvDTE.DTE _dte;
        private EnvDTE.Project _project;
        private NuPack.ProjectManager _vsProjectManager;
        private string _feed = "";

        /// <summary>
        /// Construct the type library reference provider
        /// </summary>
        /// <param name="resources"></param>
        public OnlinePackageProvider(ResourceDictionary resources, IVsProgressPane progressPane)
            : base(resources, progressPane, "Packages", "All") {
            _dte = Utilities.ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            _project = GetActiveProject(_dte);

            _vsPackageManager = NuPack.VisualStudio.VSPackageManager.GetPackageManager(_feed, _dte);
            _vsProjectManager = _vsPackageManager.GetProjectManager(_project);

            // Start the population
            //ThreadPool.QueueUserWorkItem(new WaitCallback(PopulatePackages));
            PopulatePackages(null);
        }

        /// <summary>
        /// Gather all the packages
        /// </summary>
        void PopulatePackages(object data) {
            //foreach (NuPack.Package package in _vsProjectManager.SourceRepository.GetPackages())
            IQueryable<Package> query = _vsPackageManager.ExternalRepository.GetPackages();
            foreach (NuPack.Package package in query.Take(10)) {
                PackageRecords.Add(package);
            }
        }

        internal static EnvDTE.Project GetActiveProject(EnvDTE._DTE dte) {
            EnvDTE.Project activeProject = null;

            if (dte != null) {
                Object obj = dte.ActiveSolutionProjects;
                if (obj != null && obj is Array && ((Array)obj).Length > 0) {
                    Object proj = ((Array)obj).GetValue(0);

                    if (proj != null && proj is EnvDTE.Project) {
                        activeProject = (EnvDTE.Project)proj;
                    }
                }
            }
            return activeProject;
        }


        public void Install(string id) {
            _vsProjectManager.AddPackageReference(id);
        }
    }
}

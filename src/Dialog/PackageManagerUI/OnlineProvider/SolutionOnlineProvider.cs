using System;
using System.Collections.Generic;
using System.Windows;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class SolutionOnlineProvider : OnlineProvider {
        private readonly IProjectSelectorService _projectSelector;

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

            _projectSelector = providerServices.ProjectSelector;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't want one project's failure to affect the entire operation.")]
        protected override bool ExecuteAfterLicenseAggrement(
            PackageItem item, 
            IVsPackageManager activePackageManager, 
            IList<PackageOperation> operations) {

            // hide the progress window if we are going to show project selector window
            HideProgressWindow();
            IEnumerable<Project> selectedProjects = _projectSelector.ShowProjectSelectorWindow(null);

            if (selectedProjects == null) {
                // user presses Cancel button on the Solution dialog
                return false;
            }

            ShowProgressWindow();

            var selectedProjectsSet = new HashSet<Project>(selectedProjects);
            foreach (Project project in selectedProjectsSet) {
                try {
                    ExecuteCommandOnProject(project, item, activePackageManager, operations);
                }
                catch (Exception ex) {
                    AddFailedProject(project, ex);
                }
            }
 
            return true;
        }
    }
}
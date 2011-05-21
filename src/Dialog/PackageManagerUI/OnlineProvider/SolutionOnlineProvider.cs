using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class SolutionOnlineProvider : OnlineProvider, IPackageOperationEventListener {
        private IVsPackageManager _activePackageManager;
        private IProjectSelectorService _projectSelector;

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

        protected override bool ExecuteAfterLicenseAgreement(
            PackageItem item,
            IVsPackageManager activePackageManager,
            IList<PackageOperation> operations) {

            _activePackageManager = activePackageManager;
            IEnumerable<Project> selectedProjects;

            if (activePackageManager.IsProjectLevel(item.PackageIdentity)) {
                // hide the progress window if we are going to show project selector window
                HideProgressWindow();
                selectedProjects = _projectSelector.ShowProjectSelectorWindow(ignored => true);
                if (selectedProjects == null) {
                    // user presses Cancel button on the Solution dialog
                    return false;
                }
                ShowProgressWindow();
            }
            else {
                // solution package. just install into the solution
                selectedProjects = Enumerable.Empty<Project>();
            }

            activePackageManager.InstallPackage(
                selectedProjects,
                item.PackageIdentity,
                operations,
                ignoreDependencies: false,
                logger: this,
                packageOperationEventListener: this);

            return true;
        }

        public void OnBeforeAddPackageReference(Project project) {
            RegisterPackageOperationEvents(
                _activePackageManager,
                _activePackageManager.GetProjectManager(project));
        }

        public void OnAfterAddPackageReference(Project project) {
            UnregisterPackageOperationEvents(
                _activePackageManager,
                _activePackageManager.GetProjectManager(project));
        }

        public void OnAddPackageReferenceError(Project project, Exception exception) {
            AddFailedProject(project, exception);
        }
    }
}
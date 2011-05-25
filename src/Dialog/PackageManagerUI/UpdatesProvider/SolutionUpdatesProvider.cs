using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class SolutionUpdatesProvider : UpdatesProvider, IPackageOperationEventListener {

        private IVsPackageManager _activePackageManager;
        private readonly IProjectSelectorService _projectSelector;

        public SolutionUpdatesProvider(
            IPackageRepository localRepository,
            ResourceDictionary resources,
            IPackageRepositoryFactory packageRepositoryFactory,
            IPackageSourceProvider packageSourceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager) :
            base(
                null,
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

        protected override bool ExecuteAfterLicenseAgreement(PackageItem item, IVsPackageManager activePackageManager, IList<PackageOperation> operations) {
            _activePackageManager = activePackageManager;

            IList<Project> selectedProjectsList;

            if (activePackageManager.IsProjectLevel(item.PackageIdentity)) {
                // hide the progress window if we are going to show project selector window
                HideProgressWindow();
                var selectedProjects = _projectSelector.ShowProjectSelectorWindow(
                    // Selector function to return the initial checkbox state for a Project.
                    // We check a project if it has the current package installed by Id, but not version
                    project => activePackageManager.GetProjectManager(project).LocalRepository.Exists(item.Id)
                );

                if (selectedProjects == null) {
                    // user presses Cancel button on the Solution dialog
                    return false;
                }

                selectedProjectsList = selectedProjects.ToList();
                if (selectedProjectsList.Count == 0) {
                    return false;
                }

                ShowProgressWindow();
            }
            else {
                // solution package. just update into the solution
                selectedProjectsList = new Project[0];
            }

            activePackageManager.UpdatePackage(
                selectedProjectsList,
                item.PackageIdentity,
                operations,
                updateDependencies: true,
                logger: this,
                packageOperationEventListener: this);

            return true;
        }

        protected override void ExecuteCommand(IProjectManager projectManager, PackageItem item, IVsPackageManager activePackageManager, IList<PackageOperation> operations) {
            Debug.Fail("This method should not get called.");
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
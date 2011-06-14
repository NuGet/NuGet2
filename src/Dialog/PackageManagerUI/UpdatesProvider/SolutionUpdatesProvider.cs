using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class SolutionUpdatesProvider : UpdatesProvider, IPackageOperationEventListener {

        private IVsPackageManager _activePackageManager;
        private readonly IUserNotifierServices _userNotifierServices;

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
            _userNotifierServices = providerServices.WindowServices;
        }

        protected override bool ExecuteCore(PackageItem item) {
            _activePackageManager = GetActivePackageManager();

            ShowProgressWindow();
            IList<Project> selectedProjectsList;
            if (_activePackageManager.IsProjectLevel(item.PackageIdentity)) {
                HideProgressWindow();
                var selectedProjects = _userNotifierServices.ShowProjectSelectorWindow(
                    Resources.Dialog_UpdatesSolutionInstruction,
                    // Selector function to return the initial checkbox state for a Project.
                    // We check a project if it has the current package installed by Id, but not version
                    project => _activePackageManager.GetProjectManager(project).LocalRepository.Exists(item.Id),
                    project => {
                        var localRepository = _activePackageManager.GetProjectManager(project).LocalRepository;

                        // for the Updates solution dialog, we only enable a project if it has a old version of 
                        // the package installed.
                        return localRepository.Exists(item.Id) &&
                               !localRepository.Exists(item.Id, item.PackageIdentity.Version);
                    }
                );

                if (selectedProjects == null) {
                    // user presses Cancel button on the Solution dialog
                    return false;
                }

                selectedProjectsList = selectedProjects.ToList();
                if (selectedProjectsList.Count == 0) {
                    return false;
                }
            }
            else {
                // solution package. just update into the solution
                selectedProjectsList = new Project[0];
            }

            IList<PackageOperation> operations;
            bool acceptLicense = CheckPSScriptAndShowLicenseAgreement(item, _activePackageManager, out operations);
            if (!acceptLicense) {
                return false;
            }

            _activePackageManager.UpdatePackage(
                selectedProjectsList,
                item.PackageIdentity,
                operations,
                updateDependencies: true,
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
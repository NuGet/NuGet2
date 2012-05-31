using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers
{
    internal class SolutionUpdatesProvider : UpdatesProvider, IPackageOperationEventListener
    {
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
                solutionManager)
        {
            _userNotifierServices = providerServices.UserNotifierServices;
        }

        protected override bool ExecuteCore(PackageItem item)
        {
            _activePackageManager = GetActivePackageManager();
            using (_activePackageManager.SourceRepository.StartOperation(RepositoryOperationNames.Update))
            {
                ShowProgressWindow();
                IList<Project> selectedProjectsList;
                bool isProjectLevel = _activePackageManager.IsProjectLevel(item.PackageIdentity);
                if (isProjectLevel)
                {
                    HideProgressWindow();
                    var selectedProjects = _userNotifierServices.ShowProjectSelectorWindow(
                        Resources.Dialog_UpdatesSolutionInstruction,
                        item.PackageIdentity,
                        // Selector function to return the initial checkbox state for a Project.
                        // We check a project if it has the current package installed by Id, but not version
                        project => _activePackageManager.GetProjectManager(project).LocalRepository.Exists(item.Id),
                        project =>
                        {
                            var localRepository = _activePackageManager.GetProjectManager(project).LocalRepository;

                            // for the Updates solution dialog, we only enable a project if it has an old version of 
                            // the package installed.
                            return localRepository.Exists(item.Id) &&
                                   !localRepository.Exists(item.Id, item.PackageIdentity.Version);
                        }
                    );

                    if (selectedProjects == null)
                    {
                        // user presses Cancel button on the Solution dialog
                        return false;
                    }

                    selectedProjectsList = selectedProjects.ToList();
                    if (selectedProjectsList.Count == 0)
                    {
                        return false;
                    }
                }
                else
                {
                    // solution package. just update into the solution
                    selectedProjectsList = new Project[0];
                }

                IList<PackageOperation> operations;
                bool acceptLicense = isProjectLevel ? CheckPSScriptAndShowLicenseAgreement(item, selectedProjectsList, _activePackageManager, out operations)
                                                    : CheckPSScriptAndShowLicenseAgreement(item, _activePackageManager, out operations);

                if (!acceptLicense)
                {
                    return false;
                }

                if (!isProjectLevel && operations.Any())
                {
                    // When dealing with solution level packages, only the set of actions specified under operations are executed. 
                    // In such a case, no operation to uninstall the current package is specified. We'll identify the package that is being updated and
                    // explicitly add a uninstall operation.
                    var packageToUpdate = _activePackageManager.LocalRepository.FindPackage(item.Id);
                    if (packageToUpdate != null)
                    {
                        operations.Insert(0, new PackageOperation(packageToUpdate, PackageAction.Uninstall));
                    }
                }

                try
                {
                    RegisterPackageOperationEvents(_activePackageManager, null);

                    _activePackageManager.UpdatePackage(
                        selectedProjectsList,
                        item.PackageIdentity,
                        operations,
                        updateDependencies: true,
                        allowPrereleaseVersions: IncludePrerelease,
                        logger: this,
                        eventListener: this);
                }
                finally
                {
                    UnregisterPackageOperationEvents(_activePackageManager, null);
                }

                return true;
            }
        }

        protected bool CheckPSScriptAndShowLicenseAgreement(
            PackageItem item, IList<Project> projects, IVsPackageManager packageManager, out IList<PackageOperation> operations)
        {
            ShowProgressWindow();

            // combine the operations of all selected project
            var allOperations = new List<PackageOperation>();
            foreach (Project project in projects)
            {
                IProjectManager projectManager = packageManager.GetProjectManager(project);

                IList<PackageOperation> projectOperations;
                CheckInstallPSScripts(
                    item.PackageIdentity,
                    projectManager.LocalRepository,
                    packageManager.SourceRepository,
                    project.GetTargetFrameworkName(),
                    IncludePrerelease,
                    out projectOperations);

                allOperations.AddRange(projectOperations);
            }

            // reduce the operations before checking for license agreements
            operations = allOperations.Reduce();

            return ShowLicenseAgreement(packageManager, operations);
        }

        public void OnBeforeAddPackageReference(Project project)
        {
            RegisterPackageOperationEvents(null, _activePackageManager.GetProjectManager(project));
        }

        public void OnAfterAddPackageReference(Project project)
        {
            UnregisterPackageOperationEvents(null, _activePackageManager.GetProjectManager(project));
        }

        public void OnAddPackageReferenceError(Project project, Exception exception)
        {
            AddFailedProject(project, exception);
        }
    }
}
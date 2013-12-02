using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
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
            using (_activePackageManager.SourceRepository.StartOperation(RepositoryOperationNames.Update, item.Id, item.Version))
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
                        project =>
                            {
                                var localRepository = _activePackageManager.GetProjectManager(project).LocalRepository;
                                return localRepository.Exists(item.Id) && IsVersionConstraintSatisfied(item, localRepository);
                            },
                        project =>
                            {
                                var localRepository = _activePackageManager.GetProjectManager(project).LocalRepository;

                                // for the Updates solution dialog, we only enable a project if it has an old version of
                                // the package installed.
                                return localRepository.Exists(item.Id) &&
                                       !localRepository.Exists(item.Id, item.PackageIdentity.Version) &&
                                       IsVersionConstraintSatisfied(item, localRepository);
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

                    ShowProgressWindow();
                }
                else
                {
                    // solution package. just update into the solution
                    selectedProjectsList = new Project[0];
                }

                IList<PackageOperation> operations;
                bool acceptLicense = isProjectLevel ? ShowLicenseAgreement(item.PackageIdentity, _activePackageManager, selectedProjectsList, out operations)
                                                    : ShowLicenseAgreement(item.PackageIdentity, _activePackageManager, targetFramework: null, operations: out operations);

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

        private static bool IsVersionConstraintSatisfied(PackageItem item, IPackageRepository localRepository)
        {
            // honors the version constraint set in the allowedVersion attribute of packages.config file
            var constraintProvider = localRepository as IPackageConstraintProvider;
            if (constraintProvider != null)
            {
                IVersionSpec constraint = constraintProvider.GetConstraint(item.Id);
                if (constraint != null && !constraint.Satisfies(item.PackageIdentity.Version))
                {
                    return false;
                }
            }

            return true;
        }

        protected override bool ExecuteAllCore()
        {
            if (SelectedNode == null || SelectedNode.Extensions == null || SelectedNode.Extensions.Count == 0)
            {
                return false;
            }

            ShowProgressWindow();

            _activePackageManager = GetActivePackageManager();
            Debug.Assert(_activePackageManager != null);

            IDisposable action = _activePackageManager.SourceRepository.StartOperation(OperationName, mainPackageId: null, mainPackageVersion: null);

            try
            {
                IList<PackageOperation> allOperations;
                IList<IPackage> allUpdatePackagesByDependencyOrder;
                bool accepted = ShowLicenseAgreementForAllPackages(_activePackageManager, out allOperations, out allUpdatePackagesByDependencyOrder);
                if (!accepted)
                {
                    return false;
                }

                RegisterPackageOperationEvents(_activePackageManager, null);

                _activePackageManager.UpdateSolutionPackages(
                    allUpdatePackagesByDependencyOrder,
                    allOperations,
                    updateDependencies: true,
                    allowPrereleaseVersions: IncludePrerelease,
                    logger: this,
                    eventListener: this);

                return true;
            }
            finally
            {
                UnregisterPackageOperationEvents(_activePackageManager, null);
                action.Dispose();
            }
        }

        public override IVsExtension CreateExtension(IPackage package)
        {
            return new PackageItem(this, package)
            {
                CommandName = Resources.Dialog_UpdateButton
            };
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
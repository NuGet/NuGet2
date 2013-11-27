using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers
{
    internal class SolutionOnlineProvider : OnlineProvider, IPackageOperationEventListener
    {
        private IVsPackageManager _activePackageManager;
        private readonly IUserNotifierServices _userNotifierServices;
        private readonly ISolutionManager _solutionManager;
        private static readonly Dictionary<string, bool> _checkStateCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

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
                solutionManager)
        {
            _userNotifierServices = providerServices.UserNotifierServices;
            _solutionManager = solutionManager;
        }

        public override IEnumerable<string> SupportedFrameworks
        {
            get
            {
                return from p in _solutionManager.GetProjects()
                       let fx = p.GetTargetFramework()
                       where fx != null
                       select fx;
            }
        }

        protected override bool ExecuteCore(PackageItem item)
        {
            _activePackageManager = GetActivePackageManager();
            using (_activePackageManager.SourceRepository.StartOperation(RepositoryOperationNames.Install, item.Id, item.Version))
            {
                IList<Project> selectedProjectsList;

                ShowProgressWindow();
                bool isProjectLevel = _activePackageManager.IsProjectLevel(item.PackageIdentity);
                if (isProjectLevel)
                {
                    HideProgressWindow();
                    var selectedProjects = _userNotifierServices.ShowProjectSelectorWindow(
                        Resources.Dialog_OnlineSolutionInstruction,
                        item.PackageIdentity,
                        DetermineProjectCheckState,
                        ignored => true);
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

                    // save the checked state of projects so that we can restore them the next time
                    SaveProjectCheckStates(selectedProjectsList);

                    ShowProgressWindow();
                }
                else
                {
                    // solution package. just install into the solution
                    selectedProjectsList = new Project[0];
                }

                IList<PackageOperation> operations;
                bool acceptLicense = isProjectLevel ? ShowLicenseAgreement(item.PackageIdentity, _activePackageManager, selectedProjectsList, out operations)
                                                    : ShowLicenseAgreement(item.PackageIdentity, _activePackageManager, targetFramework: null, operations: out operations);
                if (!acceptLicense)
                {
                    return false;
                }

                try
                {
                    RegisterPackageOperationEvents(_activePackageManager, null);

                    _activePackageManager.InstallPackage(
                        selectedProjectsList,
                        item.PackageIdentity,
                        operations,
                        ignoreDependencies: false,
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

        private void SaveProjectCheckStates(IList<Project> selectedProjects)
        {
            var selectedProjectSet = new HashSet<Project>(selectedProjects);

            foreach (Project project in _solutionManager.GetProjects())
            {
                if (!String.IsNullOrEmpty(project.GetUniqueName()))
                {
                    bool checkState = selectedProjectSet.Contains(project);
                    _checkStateCache[project.GetUniqueName()] = checkState;
                }
            }
        }

        private static bool DetermineProjectCheckState(Project project)
        {
            bool checkState;
            if (String.IsNullOrEmpty(project.GetUniqueName()) ||
                !_checkStateCache.TryGetValue(project.GetUniqueName(), out checkState))
            {
                checkState = true;
            }
            return checkState;
        }

        public void OnBeforeAddPackageReference(Project project)
        {
            RegisterPackageOperationEvents(
                null,
                _activePackageManager.GetProjectManager(project));
        }

        public void OnAfterAddPackageReference(Project project)
        {
            UnregisterPackageOperationEvents(
                null,
                _activePackageManager.GetProjectManager(project));
        }

        public void OnAddPackageReferenceError(Project project, Exception exception)
        {
            AddFailedProject(project, exception);
        }
    }
}
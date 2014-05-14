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
                    // solution package. just install into the active project.
                    selectedProjectsList = new Project[] { _solutionManager.DefaultProject };
                }

                // resolve operations
                var resolveResult = ResolveOperationsForInstall(
                    item.PackageIdentity,  
                    _activePackageManager, 
                    selectedProjectsList);
                var operations = resolveResult.Item1;
                bool acceptLicense = ShowLicenseAgreement(operations);
                if (!acceptLicense)
                {
                    return false;
                }

                // execute operations
                try
                {
                    RegisterPackageOperationEvents(_activePackageManager, null);

                    var userOperationExecutor = new OperationExecutor();
                    userOperationExecutor.Logger = this;
                    userOperationExecutor.PackageOperationEventListener = this;
                    userOperationExecutor.Execute(operations);
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

        public void OnBeforeAddPackageReference(IProjectManager projectManager)
        {
            RegisterPackageOperationEvents(null, projectManager);
        }

        public void OnAfterAddPackageReference(IProjectManager projectManager)
        {
            UnregisterPackageOperationEvents(null, projectManager);
        }

        public void OnAddPackageReferenceError(IProjectManager projectManager, Exception exception)
        {
            var projectSystem = projectManager.Project as VsProjectSystem;
            if (projectSystem != null)
            {
                AddFailedProject(projectSystem.Project, exception);
            }
        }
    }
}
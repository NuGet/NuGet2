using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Resolver;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")]
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
                    // solution package. just install into the active project.
                    selectedProjectsList = new Project[] { _solutionManager.DefaultProject };
                }

                // resolve operations
                var actionsByProject = ResolveActionsByProjectForInstall(
                    item.PackageIdentity,
                    _activePackageManager,
                    selectedProjectsList);

                // ask for license agreement
                var allActions = new List<Resolver.PackageAction>();
                foreach (var actions in actionsByProject.Values)
                {
                    allActions.AddRange(actions);
                }

                bool acceptLicense = ShowLicenseAgreement(allActions);
                if (!acceptLicense)
                {
                    return false;
                }

                var actionExecutor = new Resolver.ActionExecutor();
                actionExecutor.Logger = this;
                actionExecutor.PackageOperationEventListener = this;
                actionExecutor.CatchProjectOperationException = true;

                // execute operations by project
                foreach (var actionsForOneProject in actionsByProject)
                {
                    var projectManager = actionsForOneProject.Key;
                    var project = ((VsProjectSystem)(projectManager.Project)).Project;
                    try
                    {
                        RegisterPackageOperationEvents(_activePackageManager, projectManager);
                        actionExecutor.Execute(actionsForOneProject.Value);
                    }
                    catch (Exception ex)
                    {
                        AddFailedProject(project, ex);
                    }
                    finally
                    {
                        UnregisterPackageOperationEvents(_activePackageManager, projectManager);
                    }
                }

                return true;
            }
        }


        // Resolve actions to update all packages in all projects.
        private IEnumerable<Resolver.PackageAction> ResolveActionsForUpdateAll(IVsPackageManager activePackageManager)
        {
            var resolver = new ActionResolver()
            {
                Logger = this,
                AllowPrereleaseVersions = IncludePrerelease,
                DependencyVersion = activePackageManager.DependencyVersion
            };
            var allPackages = SelectedNode.GetPackages(String.Empty, IncludePrerelease);
            foreach (Project project in _solutionManager.GetProjects())
            {
                IProjectManager projectManager = activePackageManager.GetProjectManager(project);
                foreach (var package in allPackages)
                {
                    // Update if an older version package is installed in the project
                    var localPackge = projectManager.LocalRepository.FindPackage(package.Id);
                    if (localPackge != null && localPackge.Version < package.Version)
                    {
                        resolver.AddOperation(PackageAction.Install, package, projectManager);
                    }
                }
            }
            var actions = resolver.ResolveActions();
            return actions;
        }

        protected override bool ExecuteAllCore()
        {
            if (SelectedNode == null || SelectedNode.Extensions == null || SelectedNode.Extensions.Count == 0)
            {
                return false;
            }

            ShowProgressWindow();

            IVsPackageManager activePackageManager = GetActivePackageManager();
            Debug.Assert(activePackageManager != null);

            using (IDisposable action = activePackageManager.SourceRepository.StartOperation(OperationName, mainPackageId: null, mainPackageVersion: null))
            {
                var actions = ResolveActionsForUpdateAll(activePackageManager);
                bool accepted = this.ShowLicenseAgreement(actions);
                if (!accepted)
                {
                    return false;
                }

                try
                {
                    RegisterPackageOperationEvents(activePackageManager, null);

                    var actionExecutor = new ActionExecutor()
                    {
                        Logger = this
                    };
                    actionExecutor.Execute(actions);

                    return true;
                }
                finally
                {
                    UnregisterPackageOperationEvents(activePackageManager, null);
                }
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

        public override IVsExtension CreateExtension(IPackage package)
        {
            return new PackageItem(this, package)
            {
                CommandName = Resources.Dialog_UpdateButton
            };
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
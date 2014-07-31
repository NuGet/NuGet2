using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Resolver;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers
{
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of installed packages which will be shown in the Add Package dialog.
    /// </summary>
    internal class SolutionInstalledProvider : InstalledProvider
    {
        private readonly IUserNotifierServices _userNotifierServices;
        private PackageItem _lastExecutionItem;

        public SolutionInstalledProvider(
            IVsPackageManager packageManager,
            IPackageRepository localRepository,
            ResourceDictionary resources,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager,
            IPackageRestoreManager packageRestoreManager)
            : base(packageManager, null, localRepository, resources, providerServices, progressProvider, solutionManager, packageRestoreManager)
        {
            _userNotifierServices = providerServices.UserNotifierServices;
        }

        protected override void FillRootNodes()
        {
            var allNode = new SimpleTreeNode(
                this,
                Resources.Dialog_RootNodeAll,
                RootNode,
                LocalRepository,
                collapseVersion: false);
            RootNode.Nodes.Add(allNode);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want one failed project to affect the other projects.")]
        protected override bool ExecuteCore(PackageItem item)
        {
            IPackage package = item.PackageIdentity;

            // treat solution-level packages specially
            if (!PackageManager.IsProjectLevel(package))
            {
                return UninstallSolutionPackage(package);
            }

            // display the Manage dialog to allow user to pick projects to install/uninstall
            IEnumerable<Project> selectedProjects = _userNotifierServices.ShowProjectSelectorWindow(
                Resources.Dialog_InstalledSolutionInstruction,
                item.PackageIdentity,
                // Selector function to return the initial checkbox state for a Project.
                // We check a project by default if it has the current package installed.
                project => PackageManager.GetProjectManager(project).LocalRepository.Exists(package),
                ignored => true);

            if (selectedProjects == null)
            {
                // user presses Cancel button on the Solution dialog
                return false;
            }

            // bug #1181: Use HashSet<unique name> instead of HashSet<Project>.
            // in some rare cases, the project instance returned by GetProjects() may be different 
            // than the ones in selectedProjectSet.
            var selectedProjectsSet = new HashSet<string>(
                selectedProjects.Select(p => p.GetUniqueName()),
                StringComparer.OrdinalIgnoreCase);

            // now determine if user has actually made any change to the checkboxes
            IList<Project> allProjects = _solutionManager.GetProjects().ToList();

            bool hasInstallWork = allProjects.Any(p =>
                selectedProjectsSet.Contains(p.GetUniqueName()) && !IsPackageInstalledInProject(p, package));

            bool hasUninstallWork = allProjects.Any(p =>
                !selectedProjectsSet.Contains(p.GetUniqueName()) && IsPackageInstalledInProject(p, package));

            if (!hasInstallWork && !hasUninstallWork)
            {
                // nothing to do, so return
                return false;
            }

            var uninstallRepositories = new List<IPackageRepository>();
            var uninstallFrameworks = new List<FrameworkName>();
            var uninstallProjects = new List<Project>();

            bool? removeDepedencies = false;
            if (hasUninstallWork)
            {
                // Starting in 2.0, each project can have a different set of dependencies (because of different target frameworks).
                // To keep the UI simple, we aggregate all the dependencies from all uninstall projects
                // and ask if user wants to uninstall them all.

                foreach (Project project in allProjects)
                {
                    // check if user wants to uninstall the package in this project
                    if (!selectedProjectsSet.Contains(project.GetUniqueName()))
                    {
                        uninstallProjects.Add(project);
                        uninstallRepositories.Add(PackageManager.GetProjectManager(project).LocalRepository);
                        uninstallFrameworks.Add(project.GetTargetFrameworkName());
                    }
                }

                removeDepedencies = AskRemoveDependency(package, uninstallRepositories, uninstallFrameworks);
                if (removeDepedencies == null)
                {
                    // user cancels the operation.
                    return false;
                }
            }

            ShowProgressWindow();

            // now install the packages that are checked
            // Bug 1357: It's crucial that we perform all installs before uninstalls
            // to avoid the package file being deleted before an install.
            if (hasInstallWork)
            {
                bool successful = InstallPackageIntoProjects(package, allProjects, selectedProjectsSet);
                if (!successful)
                {
                    return false;
                }
            }

            // now uninstall the packages that are unchecked           
            for (int i = 0; i < uninstallProjects.Count; ++i)
            {
                try
                {
                    CheckDependentPackages(package, uninstallRepositories[i], uninstallFrameworks[i]);
                    UninstallPackageFromProject(uninstallProjects[i], item, (bool)removeDepedencies);
                }
                catch (Exception ex)
                {
                    AddFailedProject(uninstallProjects[i], ex);
                }
            }

            HideProgressWindow();
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want one failed project to affect the other projects.")]
        private bool InstallPackageIntoProjects(IPackage package, IList<Project> allProjects, HashSet<string> selectedProjectsSet)
        {
            // resolve operations
            var selectedProjects = allProjects.Where(p => selectedProjectsSet.Contains(p.GetUniqueName()));
            var actionsByProject = ResolveActionsByProjectForInstall(package, PackageManager, selectedProjects);

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

            // execute operations by project
            var actionExecutor = new ActionExecutor();
            actionExecutor.Logger = this;

            foreach (var actionsForOneProject in actionsByProject)
            {
                var projectManager = actionsForOneProject.Key;
                var project = ((VsProjectSystem)(projectManager.Project)).Project;
                try
                {
                    RegisterPackageOperationEvents(PackageManager, projectManager);
                    actionExecutor.Execute(actionsForOneProject.Value);
                }
                catch (Exception ex)
                {
                    AddFailedProject(project, ex);
                }
                finally
                {
                    UnregisterPackageOperationEvents(PackageManager, projectManager);
                }
            }

            return true;
        }

        private bool UninstallSolutionPackage(IPackage package)
        {
            CheckDependentPackages(package, LocalRepository, targetFramework: null);
            bool? result = AskRemoveDependency(
                package,
                new[] { LocalRepository },
                new FrameworkName[] { null });

            if (result == null)
            {
                // user presses Cancel
                return false;
            }

            ShowProgressWindow();
            try
            {
                RegisterPackageOperationEvents(PackageManager, null);
                
                // resolve actions
                var resolver = new ActionResolver()
                {
                    Logger = this,
                    ForceRemove = false,
                    RemoveDependencies = (bool)result
                };
                resolver.AddOperation(
                    PackageAction.Uninstall,
                    package,
                    new NullProjectManager(PackageManager));
                var actions = resolver.ResolveActions();

                // execute actions
                var actionExecutor = new ActionExecutor()
                {
                    Logger = this
                };
                actionExecutor.Execute(actions);
            }
            finally
            {
                UnregisterPackageOperationEvents(PackageManager, null);
            }
            return true;
        }

        private bool IsPackageInstalledInProject(Project project, IPackage package)
        {
            IProjectManager projectManager = PackageManager.GetProjectManager(project);
            return projectManager != null && projectManager.LocalRepository.Exists(package);
        }

        public override IVsExtension CreateExtension(IPackage package)
        {
            IEnumerable<Project> projects = GetReferenceProjects(package);
            if (projects.IsEmpty())
            {
                var repository = PackageManager.LocalRepository as ISharedPackageRepository;
                if (repository != null && !repository.IsSolutionReferenced(package.Id, package.Version))
                {
                    return null;
                }
            }

            string commandText = PackageManager.IsProjectLevel(package) ?
                Resources.Dialog_SolutionManageButton :
                Resources.Dialog_UninstallButton;

            return new PackageItem(this, package, projects)
            {
                CommandName = commandText
            };           
        }

        protected override void OnExecuteCompleted(PackageItem item)
        {
            _lastExecutionItem = item;
            SelectedNode.PackageLoadCompleted += SelectedNode_PackageLoadCompleted;

            // For the solution Installed provider, packages can be installed and uninstalled.
            // It's cumbersome to update the packages incrementally, so we just refresh everything.
            SelectedNode.Refresh(resetQueryBeforeRefresh: true);

            // repopulate the list of project references for all package items
            foreach (PackageItem packageItem in SelectedNode.Extensions)
            {
                packageItem.ReferenceProjects.Clear();
                packageItem.ReferenceProjects.AddRange(GetReferenceProjects(packageItem.PackageIdentity));
            }
        }

        private void SelectedNode_PackageLoadCompleted(object sender, EventArgs e)
        {
            ((PackagesTreeNodeBase)sender).PackageLoadCompleted -= SelectedNode_PackageLoadCompleted;

            if (SelectedNode == null || _lastExecutionItem == null)
            {
                return;
            }

            // find a new PackageItem that represents the same package as _lastExecutionItem does;
            PackageItem foundItem = SelectedNode.Extensions.OfType<PackageItem>().FirstOrDefault(
                p => PackageEqualityComparer.IdAndVersion.Equals(p.PackageIdentity, _lastExecutionItem.PackageIdentity));
            if (foundItem != null)
            {
                foundItem.IsSelected = true;
            }

            _lastExecutionItem = null;
        }

        protected override string GetProgressMessage(IPackage package)
        {
            return Resources.Dialog_InstallAndUninstallProgress + package.ToString();
        }

        public override string ProgressWindowTitle
        {
            get
            {
                return Resources.Dialog_InstallAndUninstallProgress;
            }
        }

        public override string NoItemsMessage
        {
            get
            {
                return Resources.Dialog_SolutionInstalledProviderNoItem;
            }
        }

        /// <summary>
        /// Get a list of projects which has the specified package installed.
        /// </summary>
        private IEnumerable<Project> GetReferenceProjects(IPackage package)
        {
            return from project in _solutionManager.GetProjects()
                   let projectManager = PackageManager.GetProjectManager(project)
                   where projectManager.LocalRepository.Exists(package)
                   orderby project.Name
                   select project;
        }
    }
}
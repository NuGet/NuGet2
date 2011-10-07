using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of installed packages which will be shown in the Add Package dialog.
    /// </summary>
    internal class SolutionInstalledProvider : InstalledProvider {

        private readonly ISolutionManager _solutionManager;
        private readonly IUserNotifierServices _userNotifierServices;
        private PackageItem _lastExecutionItem;

        public SolutionInstalledProvider(
            IVsPackageManager packageManager,
            IPackageRepository localRepository,
            ResourceDictionary resources,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager)
            : base(packageManager, null, localRepository, resources, providerServices, progressProvider, solutionManager) {

            _solutionManager = solutionManager;
            _userNotifierServices = providerServices.WindowServices;
        }

        public override bool CanExecute(PackageItem item) {
            return true;
        }

        protected override void FillRootNodes() {
            var allNode = new SimpleTreeNode(
                this,
                Resources.Dialog_RootNodeAll,
                RootNode,
                LocalRepository,
                collapseVersion: false);
            RootNode.Nodes.Add(allNode);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"), System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't want one failed project to affect the other projects.")]
        protected override bool ExecuteCore(PackageItem item) {
            IPackage package = item.PackageIdentity;

            bool? removeDepedencies = false;

            // treat solution-level packages specially
            if (!PackageManager.IsProjectLevel(item.PackageIdentity)) {
                removeDepedencies = AskRemoveDependencyAndCheckUninstallPSScript(package, checkDependents: true);
                if (removeDepedencies == null) {
                    // user presses Cancel
                    return false;
                }

                ShowProgressWindow();
                try {
                    RegisterPackageOperationEvents(PackageManager, null);
                    PackageManager.UninstallPackage(
                        null,
                        item.PackageIdentity.Id,
                        item.PackageIdentity.Version,
                        forceRemove: false,
                        removeDependencies: (bool)removeDepedencies,
                        logger: this);
                }
                finally {
                    UnregisterPackageOperationEvents(PackageManager, null);
                }
                return true;
            }

            // display the Manage dialog to allow user to pick projects to install/uninstall
            IEnumerable<Project> selectedProjects = _userNotifierServices.ShowProjectSelectorWindow(
                Resources.Dialog_InstalledSolutionInstruction,
                item.PackageIdentity,
                // Selector function to return the initial checkbox state for a Project.
                // We check a project by default if it has the current package installed.
                project => PackageManager.GetProjectManager(project).IsInstalled(package),
                ignored => true);

            if (selectedProjects == null) {
                // user presses Cancel button on the Solution dialog
                return false;
            }

            // bug #1181: Use HashSet<unique name> instead of HashSet<Project>.
            // in some rare cases, the project instance returned by GetProjects() may be different 
            // than the ones in selectedProjectSet.
            var selectedProjectsSet = new HashSet<string>(
                selectedProjects.Select(p => p.UniqueName),
                StringComparer.OrdinalIgnoreCase);

            // now determine if user has actually made any change to the checkboxes
            IList<Project> allProjects = _solutionManager.GetProjects().ToList();

            bool hasInstallWork = allProjects.Any(p =>
                selectedProjectsSet.Contains(p.UniqueName) && !IsPackageInstalledInProject(p, package));

            bool hasUninstallWork = allProjects.Any(p =>
                !selectedProjectsSet.Contains(p.UniqueName) && IsPackageInstalledInProject(p, package));

            if (!hasInstallWork && !hasUninstallWork) {
                // nothing to do, so return
                return false;
            }

            if (hasInstallWork) {
                IList<PackageOperation> operations;
                CheckInstallPSScripts(package, PackageManager.SourceRepository, includePrerelease: true, operations: out operations);
            }

            if (hasUninstallWork) {
                removeDepedencies = AskRemoveDependencyAndCheckUninstallPSScript(package, checkDependents: false);
                if (removeDepedencies == null) {
                    // user cancels the operation.
                    return false;
                }
            }

            ShowProgressWindow();

            // now install the packages that are checked
            // Bug 1357: It's crucial that we perform all installs before uninstalls
            // to avoid the package file being deleted before an install.
            foreach (Project project in allProjects) {
                try {
                    if (selectedProjectsSet.Contains(project.UniqueName)) {
                        // if the project is checked, install package into it  
                        InstallPackageToProject(project, item, includePrerelease: true);
                    }
                }
                catch (Exception ex) {
                    AddFailedProject(project, ex);
                }
            }

            // now uninstall the packages that are unchecked.            
            foreach (Project project in allProjects) {
                try {
                    if (!selectedProjectsSet.Contains(project.UniqueName)) {
                        // if the project is unchecked, uninstall package from it
                        UninstallPackageFromProject(project, item, (bool)removeDepedencies);
                    }
                }
                catch (Exception ex) {
                    AddFailedProject(project, ex);
                }
            }

            HideProgressWindow();
            return true;
        }

        private bool IsPackageInstalledInProject(Project project, IPackage package) {
            IProjectManager projectManager = PackageManager.GetProjectManager(project);
            return projectManager != null && projectManager.IsInstalled(package);
        }

        public override IVsExtension CreateExtension(IPackage package) {
            string commandText = PackageManager.IsProjectLevel(package) ?
                Resources.Dialog_SolutionManageButton :
                Resources.Dialog_UninstallButton;

            return new PackageItem(this, package, GetReferenceProjects(package)) {
                CommandName = commandText
            };
        }

        protected override void OnExecuteCompleted(PackageItem item) {
            _lastExecutionItem = item;
            SelectedNode.PackageLoadCompleted += SelectedNode_PackageLoadCompleted;

            // For the solution Installed provider, packages can be installed and uninstalled.
            // It's cumbersome to update the packages incrementally, so we just refresh everything.
            SelectedNode.ResetQuery();
            SelectedNode.Refresh();
        }

        private void SelectedNode_PackageLoadCompleted(object sender, EventArgs e) {
            ((PackagesTreeNodeBase)sender).PackageLoadCompleted -= SelectedNode_PackageLoadCompleted;
            
            if (SelectedNode == null || _lastExecutionItem == null) {
                return;
            }

            // find a new PackageItem that represents the same package as _lastExecutionItem does;
            PackageItem foundItem = SelectedNode.Extensions.OfType<PackageItem>().FirstOrDefault(
                p => PackageEqualityComparer.IdAndVersion.Equals(p.PackageIdentity, _lastExecutionItem.PackageIdentity));
            if (foundItem != null) {
                foundItem.IsSelected = true;
            }

            _lastExecutionItem = null;
        }

        protected override string GetProgressMessage(IPackage package) {
            return Resources.Dialog_InstallAndUninstallProgress + package.ToString();
        }

        public override string ProgressWindowTitle {
            get {
                return Resources.Dialog_InstallAndUninstallProgress;
            }
        }

        /// <summary>
        /// Get a list of projects which has the specified package installed.
        /// </summary>
        private IEnumerable<Project> GetReferenceProjects(IPackage package) {
            return from project in _solutionManager.GetProjects()
                   let projectManager = PackageManager.GetProjectManager(project)
                   where projectManager.IsInstalled(package)
                   orderby project.Name
                   select project;
        }
    }
}
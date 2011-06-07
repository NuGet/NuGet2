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
        private readonly IWindowServices _windowServices;

        public SolutionInstalledProvider(
            IVsPackageManager packageManager,
            IPackageRepository localRepository,
            ResourceDictionary resources,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager)
            : base(packageManager, null, localRepository, resources, providerServices, progressProvider, solutionManager) {

            _solutionManager = solutionManager;
            _windowServices = providerServices.WindowServices;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't want one failed project to affect the other projects.")]
        protected override bool ExecuteCore(PackageItem item) {
            IPackage package = item.PackageIdentity;

            bool removeDepedencies = false;

            // treat solution-level packages specially
            if (!PackageManager.IsProjectLevel(item.PackageIdentity)) {
                removeDepedencies = AskRemoveDependencyAndCheckPSScript(package);
                
                ShowProgressWindow();
                try {
                    RegisterPackageOperationEvents(PackageManager, null);
                    PackageManager.UninstallPackage(
                        null,
                        item.PackageIdentity.Id,
                        item.PackageIdentity.Version,
                        forceRemove: false,
                        removeDependencies: removeDepedencies,
                        logger: this);
                }
                finally {
                    UnregisterPackageOperationEvents(PackageManager, null);
                }
                return true;
            }

            // display the Manage dialog to allow user to pick projects to install/uninstall
            IEnumerable<Project> selectedProjects = _windowServices.ShowProjectSelectorWindow(
                Resources.Dialog_InstalledSolutionInstruction,
                // Selector function to return the initial checkbox state for a Project.
                // We check a project by default if it has the current package installed.
                project => PackageManager.GetProjectManager(project).IsInstalled(package),
                ignored => true);

            if (selectedProjects == null) {
                // user presses Cancel button on the Solution dialog
                return false;
            }

            removeDepedencies = AskRemoveDependencyAndCheckPSScript(package);

            ShowProgressWindow();

            var selectedProjectsSet = new HashSet<Project>(selectedProjects);

            // now install or uninstall the package depending on the checked states.
            foreach (Project project in _solutionManager.GetProjects()) {
                try {
                    if (selectedProjectsSet.Contains(project)) {
                        // if the project is checked, install package into it  
                        InstallPackageToProject(project, item);
                    }
                    else {
                        // if the project is unchecked, uninstall package from it
                        UninstallPackageFromProject(project, item, removeDepedencies);
                    }
                }
                catch (Exception ex) {
                    AddFailedProject(project, ex);
                }
            }

            return true;
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
            item.ReferenceProjects.Clear();

            // only remove the item if it is no longer installed into the solution
            if (!LocalRepository.Exists(item.PackageIdentity)) {
                base.OnExecuteCompleted(item);
            }
            else {
                // repopulate the list of projects that reference this package after every operation
                item.ReferenceProjects.AddRange(GetReferenceProjects(item.PackageIdentity));
            }
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
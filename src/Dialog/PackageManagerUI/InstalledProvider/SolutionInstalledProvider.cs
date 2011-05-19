using System;
using System.Collections.Generic;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.VisualStudio;
using NuGetConsole.Host.PowerShellProvider;

namespace NuGet.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of installed packages which will be shown in the Add Package dialog.
    /// </summary>
    internal class SolutionInstalledProvider : InstalledProvider {

        private ISolutionManager _solutionManager;
        private IProjectSelectorService _projectSelectorService;

        public SolutionInstalledProvider(
            IVsPackageManager packageManager,
            IPackageRepository localRepository,
            ResourceDictionary resources,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager)
            : base(packageManager, null, localRepository, resources, providerServices, progressProvider, solutionManager) {

            _solutionManager = solutionManager;
            _projectSelectorService = providerServices.ProjectSelector;
        }

        public override bool CanExecute(PackageItem item) {
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't want one failed project to affect the other projects.")]
        protected override bool ExecuteCore(PackageItem item) {
            IPackage package = item.PackageIdentity;

            // because we are not removing dependencies, we don't need to walk the graph to search for script files
            bool hasScript = package.HasPowerShellScript();
            if (hasScript && !RegistryHelper.CheckIfPowerShell2Installed()) {
                throw new InvalidOperationException(Resources.Dialog_PackageHasPSScript);
            }

            // treat solution-level packages specially
            if (!PackageManager.IsProjectLevel(item.PackageIdentity)) {
                try {
                    RegisterPackageOperationEvents(PackageManager, null);
                    PackageManager.UninstallPackage(
                        null,
                        item.PackageIdentity.Id,
                        item.PackageIdentity.Version,
                        forceRemove: false,
                        removeDependencies: false,
                        logger: this);
                }
                finally {
                    UnregisterPackageOperationEvents(PackageManager, null);
                }
                return true;
            }

            // hide progress window before we show the solution explorer
            HideProgressWindow();

            // display the Manage dialog to allow user to pick projects to install/uninstall
            IEnumerable<Project> selectedProjects = _projectSelectorService.ShowProjectSelectorWindow(
                // Selector function to return the initial checkbox state for a Project.
                // We check a project by default if it has the current package installed.
                project => PackageManager.GetProjectManager(project).IsInstalled(package));
            
            if (selectedProjects == null) {
                // user presses Cancel button on the Solution dialog
                return false;
            }

            ShowProgressWindow();
            
            var selectedProjectsSet = new HashSet<Project>(selectedProjects);

            // now install or uninstall the package depending on the checked states.
            foreach (Project project in _solutionManager.GetProjects()) {
                try {
                    if (selectedProjectsSet.Contains(project)) {
                        // if the project is checked, install package into it  
                        InstallPackageToProject(project, item.PackageIdentity
                      );
                    }
                    else {
                        // if the project is unchecked, uninstall package from it
                        UninstallPackageFromProject(project, item.PackageIdentity);
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

            return new PackageItem(this, package, null) {
                CommandName = commandText
            };
        }

        protected override void OnExecuteCompleted(PackageItem item) {
            // only remove the item if it is no longer installed into the solution
            if (!LocalRepository.Exists(item.PackageIdentity)) {
                base.OnExecuteCompleted(item);
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
    }
}
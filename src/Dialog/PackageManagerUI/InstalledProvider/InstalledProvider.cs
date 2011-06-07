using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;
using NuGetConsole.Host.PowerShellProvider;

namespace NuGet.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of installed packages which will be shown in the Add Package dialog.
    /// </summary>
    internal class InstalledProvider : PackagesProviderBase {

        private readonly IVsPackageManager _packageManager;
        private readonly Project _project;
        private readonly IWindowServices _windowServices;

        public InstalledProvider(
            IVsPackageManager packageManager,
            Project project,
            IPackageRepository localRepository,
            ResourceDictionary resources,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager)
            : base(localRepository, resources, providerServices, progressProvider, solutionManager) {

            if (packageManager == null) {
                throw new ArgumentNullException("packageManager");
            }

            _packageManager = packageManager;
            _project = project;
            _windowServices = providerServices.WindowServices;
        }

        protected IVsPackageManager PackageManager {
            get {
                return _packageManager;
            }
        }

        public override string Name {
            get {
                return Resources.Dialog_InstalledProvider;
            }
        }

        public override float SortOrder {
            get {
                return 1.0f;
            }
        }

        public override bool RefreshOnNodeSelection {
            get {
                return true;
            }
        }

        protected override IList<IVsSortDescriptor> CreateSortDescriptors() {
            return new List<IVsSortDescriptor> {
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortAscending), new[] { "Title", "Id" }, ListSortDirection.Ascending),
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortDescending), new[] { "Title", "Id" }, ListSortDirection.Descending)
                  };
        }

        protected override void FillRootNodes() {
            var allNode = new SimpleTreeNode(this, Resources.Dialog_RootNodeAll, RootNode, LocalRepository);
            RootNode.Nodes.Add(allNode);
        }

        public override bool CanExecute(PackageItem item) {
            // Enable command on a Package in the Installed provider if the package is installed.
            return LocalRepository.Exists(item.PackageIdentity);
        }

        protected override bool ExecuteCore(PackageItem item) {
            bool removeDependencies = AskRemoveDependencyAndCheckPSScript(item.PackageIdentity);
            ShowProgressWindow();
            UninstallPackageFromProject(_project, item, removeDependencies);
            HideProgressWindow();
            return true;
        }

        protected bool AskRemoveDependencyAndCheckPSScript(IPackage package) {
            var uninstallWalker = new UninstallWalker(
                LocalRepository,
                new DependentsWalker(LocalRepository),
                logger: NullLogger.Instance,
                removeDependencies: true,
                forceRemove: false) {
                ThrowOnConflicts = false
            };

            IList<PackageOperation> operations = uninstallWalker.ResolveOperations(package).ToList();
            var uninstallPackageNames = (from o in operations
                                         where o.Action == PackageAction.Uninstall && !PackageEqualityComparer.IdAndVersion.Equals(o.Package, package)
                                         select o.Package.ToString()).ToList();

            bool removeDependencies = false;
            if (uninstallPackageNames.Count > 0) {
                // show each dependency package on one line
                String packageNames = String.Join(Environment.NewLine, uninstallPackageNames);
                String message = String.Format(CultureInfo.CurrentCulture, Resources.Dialog_RemoveDependencyMessage, package)
                        + Environment.NewLine
                        + Environment.NewLine
                        + packageNames;

                removeDependencies = _windowServices.AskToRemoveDependencyPackages(message);
            }

            bool hasScriptPackages;
            if (removeDependencies) {
                // if user wants to remove dependencies, we need to check all of them for PS scripts
                var scriptPackages = from o in operations
                                     where o.Package.HasPowerShellScript()
                                     select o.Package;
                hasScriptPackages = scriptPackages.Any();
            }
            else {
                // otherwise, just check the to-be-uninstalled package
                hasScriptPackages = package.HasPowerShellScript(new string[] {PowerShellScripts.Uninstall});
            }

            if (hasScriptPackages) {
                if (!RegistryHelper.CheckIfPowerShell2Installed()) {
                    throw new InvalidOperationException(Resources.Dialog_PackageHasPSScript);
                }
            }

            return removeDependencies;
        }

        protected void InstallPackageToProject(Project project, PackageItem item) {
            IProjectManager projectManager = null;
            try {
                projectManager = PackageManager.GetProjectManager(project);
                // make sure the package is not installed in this project before proceeding
                if (!projectManager.IsInstalled(item.PackageIdentity)) {
                    RegisterPackageOperationEvents(PackageManager, projectManager);
                    PackageManager.InstallPackage(projectManager, item.Id, item.PackageIdentity.Version, ignoreDependencies: false, logger: this);
                }
            }
            finally {
                if (projectManager != null) {
                    UnregisterPackageOperationEvents(PackageManager, projectManager);
                }
            }
        }

        protected void UninstallPackageFromProject(Project project, PackageItem item, bool removeDependencies) {
            IProjectManager projectManager = null;
            try {
                projectManager = PackageManager.GetProjectManager(project);
                // make sure the package is installed in this project before proceeding
                if (projectManager.IsInstalled(item.PackageIdentity)) {
                    RegisterPackageOperationEvents(PackageManager, projectManager);
                    PackageManager.UninstallPackage(projectManager, item.Id, version: null, forceRemove: false, removeDependencies: removeDependencies, logger: this);
                }
            }
            finally {
                if (projectManager != null) {
                    UnregisterPackageOperationEvents(PackageManager, projectManager);
                }
            }
        }

        protected override void OnExecuteCompleted(PackageItem item) {
            base.OnExecuteCompleted(item);

            if (SelectedNode != null) {
                IList<IVsExtension> allExtensions = SelectedNode.Extensions;
                // if a package has been uninstalled, remove it from the Installed tab
                allExtensions.RemoveAll(extension => !LocalRepository.Exists((extension as PackageItem).PackageIdentity));

                // the PackagesTreeNodeBase caches the list of packages in each tree node. For this provider,
                // we don't want it to do so, because after every uninstall, we remove uninstalled packages.
                SelectedNode.ResetQuery();
            }
        }

        public override IVsExtension CreateExtension(IPackage package) {
            return new PackageItem(this, package) {
                CommandName = Resources.Dialog_UninstallButton
            };
        }

        public override string NoItemsMessage {
            get {
                return Resources.Dialog_InstalledProviderNoItem;
            }
        }

        public override string ProgressWindowTitle {
            get {
                return Dialog.Resources.Dialog_UninstallProgress;
            }
        }

        protected override string GetProgressMessage(IPackage package) {
            return Resources.Dialog_UninstallProgress + package.ToString();
        }
    }
}
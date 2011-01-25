using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

        private IVsPackageManager _packageManager;

        public InstalledProvider(
            IVsPackageManager packageManager, 
            Project project,
            IProjectManager projectManager, 
            ResourceDictionary resources,
            ProviderServices providerServices)
            : base(project, projectManager, resources, providerServices) {

            _packageManager = packageManager;
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
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortAscending), "Id"),
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortDescending), "Id", ListSortDirection.Descending)
                  };
        }

        protected override void FillRootNodes() {
            var allNode = new SimpleTreeNode(this, Resources.Dialog_RootNodeAll, RootNode, ProjectManager.LocalRepository);

            RootNode.Nodes.Add(allNode);
        }

        public override bool CanExecute(PackageItem item) {
            // Enable command on a Package in the Installed provider if the package is installed.
            return ProjectManager.IsInstalled(item.PackageIdentity);
        }

        protected override bool ExecuteCore(PackageItem item) {

            // because we are not removing dependencies, we don't need to walk the graph to search for script files
            bool hasScript = item.PackageIdentity.HasPowerShellScript(new string[] { "uninstall.ps1" });
            if (hasScript && !RegistryHelper.CheckIfPowerShell2Installed()) {
                throw new InvalidOperationException(Resources.Dialog_PackageHasPSScript);
            }

            try {
                RegisterPackageOperationEvents(_packageManager);
                _packageManager.UninstallPackage(ProjectManager, item.Id, version: null, forceRemove: false, removeDependencies: false, logger: this);
            }
            finally {
                UnregisterPackageOperationEvents(_packageManager);
            }
            return true;
        }

        protected override void OnExecuteCompleted(PackageItem item) {
            if (SelectedNode != null) {
                SelectedNode.Extensions.Remove((IVsExtension)item);
            }
        }

        public override IVsExtension CreateExtension(IPackage package) {
            return new PackageItem(this, package, null) {
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
    }
}

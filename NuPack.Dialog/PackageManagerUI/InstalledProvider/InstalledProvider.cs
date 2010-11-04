using System.ComponentModel;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of installed packages which will be shown in the Add Package dialog.
    /// </summary>
    internal class InstalledProvider : PackagesProviderBase {

        private IVsPackageManager _packageManager;

        public InstalledProvider(IVsPackageManager packageManager, IProjectManager projectManager, ResourceDictionary resources)
            : base(projectManager, resources) {
            _packageManager = packageManager;
        }

        public override string Name {
            get {
                return Resources.Dialog_InstalledProvider;
            }
        }

        public override bool RefreshOnNodeSelection {
            get {
                return true;
            }
        }

        protected override void FillRootNodes() {
            var allNode = new SimpleTreeNode(this, Resources.Dialog_RootNodeAll, RootNode, ProjectManager.LocalRepository);

            RootNode.Nodes.Add(allNode);
        }

        public override bool CanExecute(PackageItem item) {
            // enable command on a Package in the Installed provider if the package is installed.
            return ProjectManager.LocalRepository.Exists(item.PackageIdentity);
        }

        protected override bool ExecuteCore(PackageItem item, ILicenseWindowOpener licenseWindowOpener) {
            _packageManager.UninstallPackage(ProjectManager, item.Id, version: null, forceRemove: false, removeDependencies: false);
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
    }
}

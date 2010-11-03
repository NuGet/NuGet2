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

        public InstalledProvider(IVsPackageManager packageManager, IProjectManager projectManager, ResourceDictionary resources)
            : base(packageManager, projectManager, resources) {
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope")]
        public override void Execute(PackageItem item, ILicenseWindowOpener licenseWindowOpener) {
            if (OperationCoordinator.IsBusy) {
                return;
            }

            OperationCoordinator.IsBusy = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnUninstallCompleted);
            worker.DoWork += new DoWorkEventHandler(DoUninstallAsync);
            worker.RunWorkerAsync(item);
        }

        private void DoUninstallAsync(object sender, DoWorkEventArgs e) {
            PackageItem item = (PackageItem)e.Argument;
            PackageManager.UninstallPackage(ProjectManager, item.Id, version: null, forceRemove: false, removeDependencies: false);
            e.Result = item;
        }

        private void OnUninstallCompleted(object sender, RunWorkerCompletedEventArgs e) {
            OperationCoordinator.IsBusy = false;

            if (e.Error == null) {
                if (!e.Cancelled) {
                    if (SelectedNode != null) {
                        SelectedNode.Extensions.Remove((IVsExtension)e.Result);
                    }
                }
            }
            else {
                MessageHelper.ShowErrorMessage(e.Error);
            }
        }

        public override IVsExtension CreateExtension(IPackage package) {
            return new PackageItem(this, package, null) {
                CommandName = Resources.Dialog_UninstallButton
            };
        }
    }
}

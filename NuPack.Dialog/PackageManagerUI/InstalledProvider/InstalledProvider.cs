using System.Windows;
using NuPack.VisualStudio;

namespace NuPack.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of installed packages which will be shown in the Add Package dialog.
    /// </summary>
    internal class InstalledProvider : PackagesProviderBase {
        private const string XamlTemplateKey = "InstalledPackageItemTemplate";

        public InstalledProvider(ProjectManager projectManager, ResourceDictionary resources)
            : base(projectManager, resources) {
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

        protected override string MediumIconDataTemplateKey {
            get { return XamlTemplateKey; }
        }

        protected override void FillRootNodes() {
            var allNode = new SimpleTreeNode(this, Resources.Dialog_RootNodeAll, RootNode, ProjectManager.LocalRepository);

            RootNode.Nodes.Add(allNode);
        }

        public override bool GetIsCommandEnabled(PackageItem item) {
            // enable command on a Package in the Installed provider if the package is installed.
            return ProjectManager.LocalRepository.Exists(item.PackageIdentity);
        }

        // TODO: consider doing uninstall asynchronously on background thread if perf is bad, which is unlikely
        public void Uninstall(PackageItem item) {
            if (OperationCoordinator.IsBusy) {
                return;
            }

            try {
                OperationCoordinator.IsBusy = true;
                ProjectManager.RemovePackageReference(item.Id);
            }
            finally {
                OperationCoordinator.IsBusy = false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using NuPack.VisualStudio;

namespace NuPack.Dialog.Providers {
    internal class UpdatesProvider : PackagesProviderBase {
        private const string XamlTemplateKey = "UpdatePackageItemTemplate";
        private VSPackageManager _packageManager;

        public UpdatesProvider(VSPackageManager packageManager, ProjectManager projectManager, ResourceDictionary resources)
            : base(projectManager, resources) {
            _packageManager = packageManager;
        }

        public override string Name {
            get {
                return Resources.Dialog_UpdateProvider;
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
            var allNode = new UpdatesTreeNode(this, Resources.Dialog_RootNodeAll, RootNode, ProjectManager.LocalRepository, _packageManager.SourceRepository);

            RootNode.Nodes.Add(allNode);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope")]
        public void Update(PackageItem item) {
            if (OperationCoordinator.IsBusy) {
                return;
            }

            // disable all operations while this update is in progress
            OperationCoordinator.IsBusy = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnUpdateCompleted);
            worker.DoWork += new DoWorkEventHandler(DoUpdateAsync);
            worker.RunWorkerAsync(item);
        }

        private void DoUpdateAsync(object sender, DoWorkEventArgs e) {
            PackageItem item = (PackageItem)e.Argument;
            ProjectManager.UpdatePackageReference(item.Id, new Version(item.Version));
            e.Result = item;
        }

        private void OnUpdateCompleted(object sender, RunWorkerCompletedEventArgs e) {
            OperationCoordinator.IsBusy = false;

            if (e.Error == null) {
                PackageItem item = (PackageItem)e.Result;
                item.UpdateEnabledStatus();
            }
        }

        public override bool GetIsCommandEnabled(PackageItem item) {
            IPackage package = item.PackageIdentity;

            if (package == null) {
                return false;
            }
            // only enable command on a Package in the Update provider if it not updated yet.

            // the specified package can be updated if the local repository contains a package 
            // with matching id and smaller version number.
            return ProjectManager.LocalRepository.GetPackages().Any(
                p => p.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) && p.Version < package.Version);
        }

        // TODO: Temporary. Will add a helper method in Core to replace this.
        public IEnumerable<IPackage> GetPackageDependencyGraph(IPackage rootPackage) {
            HashSet<IPackage> packageGraph = new HashSet<IPackage>();
            if (DTEExtensions.DTE.Solution.IsOpen) {

                EventHandler<PackageOperationEventArgs> handler = (s, o) => {
                    o.Cancel = true;
                    packageGraph.Add(o.Package);
                };

                try {
                    _packageManager.PackageInstalling += handler;
                    _packageManager.InstallPackage(rootPackage, ignoreDependencies: false);
                }
                finally {
                    _packageManager.PackageInstalling -= handler;
                }
            }
            return packageGraph;
        }
    }
}
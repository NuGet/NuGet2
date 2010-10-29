using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class UpdatesProvider : PackagesProviderBase {

        public UpdatesProvider(IVsPackageManager packageManager, IProjectManager projectManager, ResourceDictionary resources)
            : base(packageManager, projectManager, resources) {
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

        protected override void FillRootNodes() {
            var allNode = new UpdatesTreeNode(
                this,
                Resources.Dialog_RootNodeAll,
                RootNode,
                ProjectManager.LocalRepository,
                PackageManager.SourceRepository);

            RootNode.Nodes.Add(allNode);
        }

        public override bool CanExecute(PackageItem item) {
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope")]
        public override void Execute(PackageItem item, ILicenseWindowOpener licenseWindowOpener) {
            if (OperationCoordinator.IsBusy) {
                return;
            }

            // display license window if necessary
            DependencyResolver helper = new DependencyResolver(PackageManager.SourceRepository);
            IEnumerable<IPackage> licensePackages = helper.GetDependencies(item.PackageIdentity).Where(p => p.RequireLicenseAcceptance);
            if (licensePackages.Any()) {
                bool accepted = licenseWindowOpener.ShowLicenseWindow(licensePackages);
                if (!accepted) {
                    return;
                }
            }

            // disable all other operations while this update is in progress
            OperationCoordinator.IsBusy = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnUpdateCompleted);
            worker.DoWork += new DoWorkEventHandler(DoUpdateAsync);
            worker.RunWorkerAsync(item);
        }

        private void DoUpdateAsync(object sender, DoWorkEventArgs e) {
            PackageItem item = (PackageItem)e.Argument;
            PackageManager.UpdatePackage(ProjectManager, item.Id, new Version(item.Version), updateDependencies: true);
            e.Result = item;
        }

        private void OnUpdateCompleted(object sender, RunWorkerCompletedEventArgs e) {
            OperationCoordinator.IsBusy = false;

            if (e.Error == null) {
                PackageItem item = (PackageItem)e.Result;
                item.UpdateEnabledStatus();
            }

            if (UpdateCompletedCallback != null) {
                UpdateCompletedCallback();
            }
        }

        // hook for unit test
        internal Action UpdateCompletedCallback { get; set; }

        public override IVsExtension CreateExtension(IPackage package) {
            return new PackageItem(this, package, null) {
                CommandName = Resources.Dialog_UpdateButton
            };
        }
    }
}

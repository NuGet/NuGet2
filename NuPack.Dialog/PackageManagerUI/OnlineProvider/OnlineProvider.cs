using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of packages from a package feed which will be shown in the Add NuGet dialog.
    /// </summary>
    internal class OnlineProvider : PackagesProviderBase {
        private IPackageRepositoryFactory _packageRepositoryFactory;
        private IPackageSourceProvider _packageSourceProvider;

        public OnlineProvider(
            IVsPackageManager packageManager, 
            IProjectManager projectManager, 
            ResourceDictionary resources, 
            IPackageRepositoryFactory packageRepositoryFactory, 
            IPackageSourceProvider packageSourceProvider) :
            base(packageManager, projectManager, resources) {

            _packageRepositoryFactory = packageRepositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        public override string Name {
            get {
                return Resources.Dialog_OnlineProvider;
            }
        }

        public override bool RefreshOnNodeSelection {
            get {
                // only refresh if the current node doesn't have any extensions
                return (SelectedNode == null || SelectedNode.Extensions.Count == 0);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design", 
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification="We want to suppress all errors to show an empty node.")]
        protected override void FillRootNodes() {
            var packageSources = _packageSourceProvider.GetPackageSources();

            // create one tree node per package source
            // REVIEW: do we want to truncate the number of nodes?
            foreach (var source in packageSources) {
                PackagesTreeNodeBase node = null;
                try {
                    IPackageRepository repository = _packageRepositoryFactory.CreateRepository(source.Source);
                    node = new SimpleTreeNode(this, source.Name, RootNode, repository);
                }
                catch (Exception) {
                    // exception occurs if the Source value is invalid. In which case, adds an empty tree node in place.
                    node = new EmptyTreeNode(this, source.Name, RootNode);
                }

                RootNode.Nodes.Add(node);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
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

            // disable all operations while this install is in progress
            OperationCoordinator.IsBusy = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(DoInstallAsync);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnInstallCompleted);
            worker.RunWorkerAsync(item);
        }

        private void DoInstallAsync(object sender, DoWorkEventArgs e) {
            PackageItem item = (PackageItem)e.Argument;
            PackageManager.InstallPackage(ProjectManager, item.Id, new Version(item.Version), ignoreDependencies: false);
            e.Result = item;
        }

        private void OnInstallCompleted(object sender, RunWorkerCompletedEventArgs e) {
            OperationCoordinator.IsBusy = false;

            if (e.Error == null) {
                PackageItem item = (PackageItem)e.Result;
                item.UpdateEnabledStatus();
            }

            if (InstallCompletedCallback != null) {
                InstallCompletedCallback();
            }
        }

        public override bool CanExecute(PackageItem item) {
            // only enable command on a Package in the Online provider if it is not installed yet
            return !ProjectManager.LocalRepository.Exists(item.Id, new Version(item.Version));
        }

        public override IVsExtension CreateExtension(IPackage package) {
            return new PackageItem(this, package, null) {
                CommandName = Resources.Dialog_InstallButton
            };
        }

        // hook for unit test
        internal Action InstallCompletedCallback { get; set; }
    }
}

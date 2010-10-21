using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using NuPack.VisualStudio;

namespace NuPack.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of packages from a package feed which will be shown in the Add NuPack dialog.
    /// </summary>
    internal class OnlineProvider : PackagesProviderBase {

        private const string XamlTemplateKey = "OnlinePackageItemTemplate";
        private IPackageRepositoryFactory _packageRepositoryFactory;
        private VSPackageSourceProvider _packageSourceProvider;
        private VSPackageManager _packageManager;

        public OnlineProvider(
            VSPackageManager packageManager,
            ProjectManager projectManager,
            IPackageRepositoryFactory packageRepositoryFactory,
            VSPackageSourceProvider packageSourceProvider,
            ResourceDictionary resources) :
            base(projectManager, resources) {

            _packageManager = packageManager;
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
                return (SelectedNode.Extensions.Count == 0);
            }
        }

        protected override string MediumIconDataTemplateKey {
            get { return XamlTemplateKey; }
        }

        protected override void FillRootNodes() {
            var packageSources = _packageSourceProvider.GetPackageSources();

            // create one tree node per package source
            // REVIEW: do we want to truncate the number of nodes?
            foreach (var source in packageSources) {

                PackagesTreeNodeBase node = null;
                try {
                    IPackageRepository repository = _packageRepositoryFactory.CreateRepository(source.Source);
                    node = new SimpleTreeNode(this, NormalizeCategory(source.Name), RootNode, repository);
                }
                catch (UriFormatException) {
                    // exception occurs if the Source value is invalid. In which case, adds the empty tree node in place
                    node = new EmptyTreeNode(this, NormalizeCategory(source.Name), RootNode);
                }

                RootNode.Nodes.Add(node);
            }
        }

        private static string NormalizeCategory(string category) {
            const int MaxCategoryLength = 50;
            if (category.Length > MaxCategoryLength) {
                return category.Substring(0, MaxCategoryLength) + "...";
            }
            else {
                return category;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public void Install(PackageItem item) {
            if (OperationCoordinator.IsBusy) {
                return;
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
            ProjectManager.AddPackageReference(item.Id, new Version(item.Version));
            e.Result = item;
        }

        private void OnInstallCompleted(object sender, RunWorkerCompletedEventArgs e) {
            OperationCoordinator.IsBusy = false;

            if (e.Error == null) {
                PackageItem item = (PackageItem)e.Result;
                item.UpdateEnabledStatus();
            }
        }

        public override bool GetIsCommandEnabled(PackageItem item) {
            // only enable command on a Package in the Online provider if it is not installed yet
            return !ProjectManager.LocalRepository.Exists(item.Id, new Version(item.Version));
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
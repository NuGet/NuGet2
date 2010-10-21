using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using NuPack.VisualStudio;

namespace NuPack.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of packages from a package feed which will be shown in the Add NuPack dialog.
    /// </summary>
    internal class OnlinePackagesProvider : VsExtensionsProvider {
        private IVsExtensionsTreeNode _searchNode;
        private readonly ResourceDictionary _resources;
        private readonly VSPackageManager _packageManager;
        private readonly EnvDTE.Project _activeProject;

        public OnlinePackagesProvider(
            VSPackageManager packageManager,
            EnvDTE.Project activeProject,
            ResourceDictionary resources) {

            if (packageManager == null) {
                throw new ArgumentNullException("packageManager");
            }

            if (activeProject == null) {
                throw new ArgumentNullException("activeProject");
            }

            if (resources == null) {
                throw new ArgumentNullException("resources");
            }

            _resources = resources;
            _packageManager = packageManager;
            _activeProject = activeProject;
        }

        public virtual bool RefreshOnNodeSelection {
            get {
                return false;
            }
        }

        protected VSPackageManager PackageManager {
            get {
                return _packageManager;
            }
        }

        protected EnvDTE.Project ActiveProject {
            get {
                return _activeProject;
            }
        }

        protected ProjectManager ProjectManager {
            get {
                return this.PackageManager.GetProjectManager(ActiveProject);
            }
        }

        protected virtual IPackageRepository PackagesRepository {
            get {
                return PackageManager.SourceRepository;
            }
        }

        public virtual IQueryable<IPackage> GetQuery() {
            return PackagesRepository.GetPackages();
        }

        /// <summary>
        /// Gets the root node of the tree
        /// </summary>
        protected virtual IVsExtensionsTreeNode RootNode {
            get;
            set;
        }

        public override IVsExtensionsTreeNode ExtensionsTree {
            get {
                if (RootNode == null) {
                    RootNode = new BasePackagesTree(null, String.Empty);
                    CreateExtensionsTree();
                }

                return RootNode;
            }
        }

        private object _mediumIconDataTemplate;
        public override object MediumIconDataTemplate {
            get {
                if (_mediumIconDataTemplate == null) {
                    _mediumIconDataTemplate = _resources["OnlinePackageItemTemplate"];
                }
                return _mediumIconDataTemplate;
            }
        }

        private object _detailViewDataTemplate;
        public override object DetailViewDataTemplate {
            get {
                if (_detailViewDataTemplate == null) {
                    _detailViewDataTemplate = _resources["PackageDetailTemplate"];
                }
                return _detailViewDataTemplate;
            }
        }

        public override string Name {
            get {
                return Resources.Dialog_OnlineProvider;
            }
        }

        public override string ToString() {
            return Name;
        }

        public override IVsExtensionsTreeNode Search(string searchTerms) {
            if (OperationCoordinator.IsBusy) {
                return null;
            }

            if (_searchNode != null) {
                // dispose any search results
                this.RootNode.Nodes.Remove(_searchNode);
                _searchNode = null;
            }

            if (!string.IsNullOrEmpty(searchTerms)) {
                _searchNode = new OnlinePackagesSearchNode(this, this.RootNode, searchTerms);
                _searchNode.IsSelected = true;
                ExtensionsTree.Nodes.Add(_searchNode);
            }

            return _searchNode;
        }

        private void CreateExtensionsTree() {
            // The user may have done a search before we finished getting the category list; temporarily remove it
            if (_searchNode != null) {
                RootNode.Nodes.Remove(_searchNode);
            }

            // Add the special "All" node which doesn't filter by category.
            RootNode.Nodes.Add(new OnlinePackagesTree(this, Resources.Dialog_RootNodeAll, RootNode));

            if (_searchNode != null) {
                // Re-add the search node and select it if the user was doing a search
                RootNode.Nodes.Add(_searchNode);
                _searchNode.IsSelected = true;
            }
            else {
                //If they weren't doing a search, select the first category.
                RootNode.Nodes.First().IsSelected = true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public void Install(OnlinePackagesItem item) {
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
            OnlinePackagesItem item = (OnlinePackagesItem)e.Argument;
            PackageManager.InstallPackage(ProjectManager, item.Id, new Version(item.Version), ignoreDependencies: false, logger: NullLogger.Instance);
            e.Result = item;
        }

        private void OnInstallCompleted(object sender, RunWorkerCompletedEventArgs e) {
            OperationCoordinator.IsBusy = false;

            if (e.Error == null) {
                OnlinePackagesItem item = (OnlinePackagesItem)e.Result;
                item.UpdateInstallStatus();
            }
        }

        public bool IsInstalled(string id, Version version) {
            return ProjectManager.LocalRepository.Exists(id, version);
        }

        public void Uninstall(OnlinePackagesItem item) {
            if (OperationCoordinator.IsBusy) {
                return;
            }

            try {
                OperationCoordinator.IsBusy = true;
                PackageManager.UninstallPackage(ProjectManager, item.Id, version: null, forceRemove: false, removeDependencies: false, logger: NullLogger.Instance);
            }
            finally {
                OperationCoordinator.IsBusy = false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public void Update(OnlinePackagesItem item) {
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
            OnlinePackagesItem item = (OnlinePackagesItem)e.Argument;
            PackageManager.UpdatePackage(ProjectManager, item.Id, new Version(item.Version), updateDependencies: true, logger: NullLogger.Instance);
            e.Result = item;
        }

        private void OnUpdateCompleted(object sender, RunWorkerCompletedEventArgs e) {
            OperationCoordinator.IsBusy = false;

            if (e.Error == null) {
                OnlinePackagesItem item = (OnlinePackagesItem)e.Result;
                item.UpdateUpdateStatus();
            }
        }

        public bool CanBeUpdated(IPackage package) {
            if (package == null) {
                return false;
            }

            // the specified package can be updated if the local repository contains a package 
            // with matching id and smaller version number.
            return ProjectManager.LocalRepository.GetPackages().Any(
                p => p.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) && p.Version < package.Version);
        }

        public IEnumerable<IPackage> GetPackageDependencyGraph(IPackage rootPackage) {
            HashSet<IPackage> packageGraph = new HashSet<IPackage>();
            if (DTEExtensions.DTE.Solution.IsOpen) {

                EventHandler<PackageOperationEventArgs> handler = (s, o) => {
                    o.Cancel = true;
                    packageGraph.Add(o.Package);
                };

                try {
                    PackageManager.PackageInstalling += handler;
                    PackageManager.InstallPackage(rootPackage, ignoreDependencies: false);
                }
                finally {
                    PackageManager.PackageInstalling -= handler;
                }
            }
            return packageGraph;
        }
    }
}
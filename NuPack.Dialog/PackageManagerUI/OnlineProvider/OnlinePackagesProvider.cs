using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    internal class OnlinePackagesProvider : VsExtensionsProvider, IVsProgressPaneConsumer {
        private IVsExtensionsTreeNode _searchNode;
        private readonly bool _onlineDisabled;
        private Dispatcher _currentDispatcher;
        private readonly ResourceDictionary _resources;
        private readonly VSPackageManager _packageManager;
        private readonly EnvDTE.Project _activeProject;
       
        public OnlinePackagesProvider(VSPackageManager packageManager, EnvDTE.Project activeProject, ResourceDictionary resources, bool onlineDisabled) {

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
            _onlineDisabled = onlineDisabled;
            _packageManager = packageManager;
            _activeProject = activeProject;
        }

        private IVsProgressPane ProgressPane { get; set; }

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
                    this._currentDispatcher = Dispatcher.CurrentDispatcher;
                    RootNode = new BasePackagesTree(null, String.Empty);
                    new Thread(new ThreadStart(CreateExtensionsTree)).Start();
                }

                return RootNode;
            }
        }

        private object _mediumIconDataTemplate;
        public override object MediumIconDataTemplate {
            get {
                if (_mediumIconDataTemplate == null) {
                    _mediumIconDataTemplate = _resources["OnlineTileTemplate"];
                }
                return _mediumIconDataTemplate;
            }
        }

        private object _detailViewDataTemplate;
        public override object DetailViewDataTemplate {
            get {
                if (_detailViewDataTemplate == null) {
                    _detailViewDataTemplate = _resources["OnlineDetailTemplate"];
                }
                return _detailViewDataTemplate;
            }
        }

        public override string Name {
            get {
                return "Online";
            }
        }

        public override string ToString() {
            return Name;
        }

        public override IVsExtensionsTreeNode Search(string searchTerms) {
            if (_onlineDisabled) return null;

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

        #region IVsProgressPaneConsumer Members

        void IVsProgressPaneConsumer.SetProgressPane(IVsProgressPane progressPane) {
            this.ProgressPane = progressPane;
        }

        #endregion

        private void CreateExtensionsTree() {
            this._currentDispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate() {
                if (this.ProgressPane != null && this.ProgressPane.Show(null, false)) {
                    this.ProgressPane.IsIndeterminate = true;
                }
            }));

            List<IVsExtensionsTreeNode> rootNodes = new List<IVsExtensionsTreeNode>();
            if (this.ProgressPane != null) {
                this.ProgressPane.Close();
            }

            if (this._currentDispatcher != null) {
                this._currentDispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new ThreadStart(delegate() {
                    //The user may have done a search before we finished getting the category list.
                    //temporarily remove it
                    if (_searchNode != null) {
                        RootNode.Nodes.Remove(_searchNode);
                    }

                    //Add the special "All" node which doesn't filter by category.
                    RootNode.Nodes.Add(new OnlinePackagesTree(this, "All", RootNode));

                    rootNodes.ForEach(node => RootNode.Nodes.Add(node));

                    if (_searchNode != null) {
                        //Re-add the search node and select it if the user was doing a search
                        RootNode.Nodes.Add(_searchNode);
                        _searchNode.IsSelected = true;
                    }
                    else {
                        //If they weren't doing a search, select the first category.
                        RootNode.Nodes.First().IsSelected = true;
                    }
                }
                ));
            }
        }

        public void Install(string id, Version version) {
            ProjectManager.AddPackageReference(id, version);
        }

        public void Uninstall(string id) {
            ProjectManager.RemovePackageReference(id);
        }

        public bool IsInstalled(string id, Version version) {
            return (ProjectManager.LocalRepository.FindPackage(id, version) != null);
        }

        public void Update(string id, Version version) {
            ProjectManager.UpdatePackageReference(id, version);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance", 
            "CA1822:MarkMembersAsStatic",
            Justification="This method is invoked from XAML.")]
        public bool CanBeUpdated(IPackage package) {
            if (package == null) {
                return false;
            }

            // the specified package can be updated if the local repository contains a package 
            // with matching id and smaller version number.
            return ProjectManager.LocalRepository.GetPackages().Any(p => p.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) && p.Version < package.Version);
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
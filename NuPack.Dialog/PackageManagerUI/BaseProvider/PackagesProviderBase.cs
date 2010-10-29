using System;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of packages from a package feed which will be shown in the Add NuGet dialog.
    /// </summary>
    internal abstract class PackagesProviderBase : VsExtensionsProvider {

        private IVsExtensionsTreeNode _searchNode;
        private readonly ResourceDictionary _resources;

        private object _mediumIconDataTemplate;
        private object _detailViewDataTemplate;

        protected PackagesProviderBase(IVsPackageManager packageManager, IProjectManager projectManager, ResourceDictionary resources) {
            if (packageManager == null) {
                throw new ArgumentNullException("packageManager");
            }

            if (projectManager == null) {
                throw new ArgumentNullException("projectManager");
            }

            if (resources == null) {
                throw new ArgumentNullException("resources");
            }

            _resources = resources;
            ProjectManager = projectManager;
            PackageManager = packageManager;
        }

        public virtual bool RefreshOnNodeSelection {
            get {
                return false;
            }
        }

        protected IVsPackageManager PackageManager {
            get;
            private set;
        }

        protected IProjectManager ProjectManager {
            get;
            private set;
        }

        public PackagesTreeNodeBase SelectedNode { 
            get; 
            set; 
        }

        /// <summary>
        /// Gets the root node of the tree
        /// </summary>
        protected IVsExtensionsTreeNode RootNode {
            get;
            set;
        }

        public override IVsExtensionsTreeNode ExtensionsTree {
            get {
                if (RootNode == null) {
                    RootNode = new RootPackagesTreeNode(null, String.Empty);
                    CreateExtensionsTree();
                }

                return RootNode;
            }
        }

        public override object MediumIconDataTemplate {
            get {
                if (_mediumIconDataTemplate == null) {
                    _mediumIconDataTemplate = _resources["PackageItemTemplate"];
                }
                return _mediumIconDataTemplate;
            }
        }

        public override object DetailViewDataTemplate {
            get {
                if (_detailViewDataTemplate == null) {
                    _detailViewDataTemplate = _resources["PackageDetailTemplate"];
                }
                return _detailViewDataTemplate;
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
                RootNode.Nodes.Remove(_searchNode);
                _searchNode = null;
            }

            if (!string.IsNullOrEmpty(searchTerms)) {
                _searchNode = new PackagesSearchNode(this, this.RootNode, SelectedNode, searchTerms) {
                    IsSelected = true
                };
                
                RootNode.Nodes.Add(_searchNode);
            }

            return _searchNode;
        }

        private void CreateExtensionsTree() {
            // The user may have done a search before we finished getting the category list; temporarily remove it
            if (_searchNode != null) {
                RootNode.Nodes.Remove(_searchNode);
            }

            // give subclass a chance to populate the child nodes under Root node
            FillRootNodes();

            if (_searchNode != null) {
                // Re-add the search node and select it if the user was doing a search
                RootNode.Nodes.Add(_searchNode);
                _searchNode.IsSelected = true;
            }
            else {
                // If they weren't doing a search, select the first category.
                var firstChild = RootNode.Nodes.FirstOrDefault();
                if (firstChild != null) {
                    firstChild.IsSelected = true;
                }
            }
        }

        protected virtual void FillRootNodes() {
        }

        public abstract IVsExtension CreateExtension(IPackage package);

        public abstract bool CanExecute(PackageItem item);

        public abstract void Execute(PackageItem item, ILicenseWindowOpener licenseWindowOpener);
    }
}

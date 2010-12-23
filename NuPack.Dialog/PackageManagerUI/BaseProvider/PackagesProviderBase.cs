using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using NuGet.Dialog.PackageManagerUI;

namespace NuGet.Dialog.Providers {
    /// <summary>
    /// Base class for all tree node types.
    /// </summary>
    internal abstract class PackagesProviderBase : VsExtensionsProvider {

        private PackagesSearchNode _searchNode;
        private PackagesTreeNodeBase _lastSelectedNode;
        private readonly ResourceDictionary _resources;
        private IProgressWindowOpener _progressWindowOpener;

        private object _mediumIconDataTemplate;
        private object _detailViewDataTemplate;
        private IList<IVsSortDescriptor> _sortDescriptors;

        protected PackagesProviderBase(
            IProjectManager projectManager, 
            ResourceDictionary resources,
            IProgressWindowOpener progressWindowOpener) {

            if (projectManager == null) {
                throw new ArgumentNullException("projectManager");
            }

            if (resources == null) {
                throw new ArgumentNullException("resources");
            }

            if (progressWindowOpener == null) {
                throw new ArgumentNullException("progressWindowOpener");
            }

            _resources = resources;
            
            _progressWindowOpener = progressWindowOpener;
            ProjectManager = projectManager;
        }

        public virtual bool RefreshOnNodeSelection {
            get {
                return false;
            }
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

        public PackageSortDescriptor CurrentSort {
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

        // hook for unit test
        internal Action ExecuteCompletedCallback {
            get;
            set;
        }

        public IList<IVsSortDescriptor> SortDescriptors {
            get {
                if (_sortDescriptors == null) {
                    _sortDescriptors = CreateSortDescriptors();
                }
                return _sortDescriptors;
            }
        }

        protected virtual IList<IVsSortDescriptor> CreateSortDescriptors() {
            return new List<IVsSortDescriptor> {
                        new PackageSortDescriptor(Resources.Dialog_SortOption_HighestRated, "Rating", ListSortDirection.Descending),
                        new PackageSortDescriptor(Resources.Dialog_SortOption_MostDownloads, "DownloadCount", ListSortDirection.Descending),
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortAscending), "Id"),
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortDescending), "Id", ListSortDirection.Descending)
                  };
        }

        public override string ToString() {
            return Name;
        }

        public override IVsExtensionsTreeNode Search(string searchText) {
            if (OperationCoordinator.IsBusy) {
                return null;
            }

            if (!String.IsNullOrEmpty(searchText) && SelectedNode != null) {
                if (_searchNode != null) {
                    _searchNode.SetSearchText(searchText);
                }
                else {
                    _searchNode = new PackagesSearchNode(this, this.RootNode, SelectedNode, searchText);
                    AddSearchNode();
                }
            }
            else {
                RemoveSearchNode();
            }

            return _searchNode;
        }

        private void RemoveSearchNode() {
            if (_searchNode != null) {
                if (_lastSelectedNode != null) {
                    // after search, we want to reset the original node to page 1 (Work Item #461) 
                    _lastSelectedNode.CurrentPage = 1;
                    SelectNode(_lastSelectedNode);
                }

                // dispose any search results
                RootNode.Nodes.Remove(_searchNode);
                _searchNode = null;
            }
        }

        private void AddSearchNode() {
            if (_searchNode != null && !RootNode.Nodes.Contains(_searchNode)) {
                // remember the currently selected node so that when search term is cleared, we can restore it.
                _lastSelectedNode = SelectedNode;

                RootNode.Nodes.Add(_searchNode);
                SelectNode(_searchNode);
            }
        }

        protected void SelectNode(PackagesTreeNodeBase node) {
            node.IsSelected = true;
            SelectedNode = node;
        }

        private void CreateExtensionsTree() {
            // The user may have done a search before we finished getting the category list; temporarily remove it
            RemoveSearchNode();

            // give subclass a chance to populate the child nodes under Root node
            FillRootNodes();

            // Re-add the search node and select it if the user was doing a search
            AddSearchNode();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public virtual void Execute(PackageItem item) {
            if (OperationCoordinator.IsBusy) {
                return;
            }

            // disable all operations while this install is in progress
            OperationCoordinator.IsBusy = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += OnRunWorkerDoWork;
            worker.RunWorkerCompleted += OnRunWorkerCompleted;

            // this allows the async operation to cancel the progress window display (in case
            // there is error or need to show license window)
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Token.Register(CloseProgressWindow, true);

            // We don't want to show progress window immediately. Instead, we set a delayed timer. 
            // After it times out, if the operation is still ongoing, then we show progress window.
            // This way, if the operation happens too fast, progress window doesn't need to show up.
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(600);
            timer.Tick += (o, e) => {
                timer.Stop();
                if (worker.IsBusy && !cts.IsCancellationRequested) {
                    _progressWindowOpener.ShowModal(ProgressWindowTitle);
                }
            };
            timer.Start();

            worker.RunWorkerAsync(Tuple.Create(item, cts));
        }

        private void CloseProgressWindow() {
            if (_progressWindowOpener.IsOpen) {
                _progressWindowOpener.Close();
            }
        }

        private void OnRunWorkerDoWork(object sender, DoWorkEventArgs e) {
            var tuple = (Tuple<PackageItem, CancellationTokenSource>)e.Argument;
            bool succeeded = ExecuteCore(tuple.Item1, tuple.Item2);
            e.Cancel = !succeeded;
            e.Result = tuple.Item1;
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            OperationCoordinator.IsBusy = false;

            // operation completed. Enable the OK button to allow user to close the dialog.
            _progressWindowOpener.SetCompleted();

            if (e.Error == null) {
                if (e.Cancelled) {
                    CloseProgressWindow();
                }
                else {
                    OnExecuteCompleted((PackageItem)e.Result);
                }
            }
            else {
                CloseProgressWindow();
                MessageHelper.ShowErrorMessage(e.Error);
            }

            if (ExecuteCompletedCallback != null) {
                ExecuteCompletedCallback();
            }
        }

        protected virtual void FillRootNodes() {
        }

        public abstract IVsExtension CreateExtension(IPackage package);

        public abstract bool CanExecute(PackageItem item);

        /// <summary>
        /// This method is called on background thread.
        /// </summary>
        /// <returns><c>true</c> if the method succeeded. <c>false</c> otherwise.</returns>
        protected virtual bool ExecuteCore(PackageItem item, CancellationTokenSource progressWindowCts) {
            return true;
        }

        protected virtual void OnExecuteCompleted(PackageItem item) {
        }

        public virtual string NoItemsMessage {
            get {
                return String.Empty;
            } 
        }

        public virtual string ProgressWindowTitle {
            get {
                return String.Empty;
            }
        }
    }
}

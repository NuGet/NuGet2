using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Internal.Web.Utils;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.Extensions;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    internal abstract class PackagesTreeNodeBase : IVsExtensionsTreeNode, IVsPageDataSource, IVsSortDataSource, IVsProgressPaneConsumer, INotifyPropertyChanged, IVsMessagePaneConsumer {

        // The number of extensions to show per page.
        private const int DefaultItemsPerPage = 10;

        // We cache the query until it changes (due to sort order or search)
        private IEnumerable<IPackage> _query;
        private int _totalCount;

        private IList<IVsExtension> _extensions;
        private IList<IVsExtensionsTreeNode> _nodes;
        private int _totalPages = 1, _currentPage = 1;
        private bool _progressPaneActive;
        private bool _isExpanded;
        private bool _isSelected;
        private bool _loadingInProgress;

        private CancellationTokenSource _currentCancellationSource;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> PageDataChanged;

        protected PackagesTreeNodeBase(IVsExtensionsTreeNode parent, PackagesProviderBase provider) {
            Debug.Assert(provider != null);

            Parent = parent;
            Provider = provider;
            PageSize = DefaultItemsPerPage;
        }

        protected PackagesProviderBase Provider {
            get;
            private set;
        }

        private IVsProgressPane ProgressPane {
            get;
            set;
        }

        private IVsMessagePane MessagePane {
            get;
            set;
        }

        /// <summary>
        /// Name of this node
        /// </summary>
        public abstract string Name {
            get;
        }

        public bool IsSearchResultsNode {
            get;
            set;
        }

        /// <summary>
        /// Select node (UI) property
        /// This property maps to TreeViewItem.IsSelected
        /// </summary>
        public bool IsSelected {
            get {
                return _isSelected;
            }
            set {
                if (_isSelected != value) {
                    _isSelected = value;
                    OnNotifyPropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        /// Expand node (UI) property
        /// This property maps to TreeViewItem.IsExpanded
        /// </summary>
        public bool IsExpanded {
            get {
                return _isExpanded;
            }
            set {
                if (_isExpanded != value) {
                    _isExpanded = value;
                    OnNotifyPropertyChanged("IsExpanded");
                }
            }
        }

        /// <summary>
        /// List of templates at this node
        /// </summary>
        public IList<IVsExtension> Extensions {
            get {
                if (_extensions == null) {
                    EnsureExtensionCollection();
                    LoadPage(1);
                }

                return _extensions;
            }
        }

        /// <summary>
        /// Children at this node
        /// </summary>
        public IList<IVsExtensionsTreeNode> Nodes {
            get {
                if (_nodes == null) {
                    _nodes = new ObservableCollection<IVsExtensionsTreeNode>();
                }
                return _nodes;
            }
        }
        /// <summary>
        /// Parent of this node
        /// </summary>
        public IVsExtensionsTreeNode Parent {
            get;
            private set;
        }

        public int TotalPages {
            get {
                return _totalPages;
            }
            internal set {
                _totalPages = value;
                NotifyPropertyChanged();
            }
        }

        public int CurrentPage {
            get {
                return _currentPage;
            }
            internal set {
                _currentPage = value;
                NotifyPropertyChanged();
            }
        }

        // this is for unit testing
        internal Action QueryExecutionCallback {
            get;
            set;
        }

        internal int PageSize {
            get;
            set;
        }

        /// <summary>
        /// Refresh the list of packages belong to this node
        /// </summary>
        public void Refresh() {
            LoadPage(CurrentPage);
        }

        public override string ToString() {
            return Name;
        }

        /// <summary>
        /// Get all packages belonging to this node.
        /// </summary>
        /// <returns></returns>
        public abstract IQueryable<IPackage> GetPackages();

        /// <summary>
        /// Helper function to raise property changed events
        /// </summary>
        /// <param name="info"></param>
        private void NotifyPropertyChanged() {
            if (PageDataChanged != null) {
                PageDataChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Loads the packages in the specified page.
        /// </summary>
        /// <param name="pageNumber"></param>
        public void LoadPage(int pageNumber) {
            if (pageNumber < 1) {
                throw new ArgumentOutOfRangeException("pageNumber", String.Format(CultureInfo.CurrentCulture, CommonResources.Argument_Must_Be_GreaterThanOrEqualTo, 1));
            }

            Trace.WriteLine("Dialog loading page: " + pageNumber);
            if (_loadingInProgress) {
                return;
            }

            EnsureExtensionCollection();

            ShowProgressPane();

            // avoid more than one loading occurring at the same time
            _loadingInProgress = true;

            _currentCancellationSource = new CancellationTokenSource();

            TaskScheduler uiScheduler = null;
            try {
                uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            catch (InvalidOperationException) {
                // FromCurrentSynchronizationContext() fails when running from unit test
                uiScheduler = TaskScheduler.Default;
            }

            Task.Factory.StartNew(
                (state) => ExecuteAsync(pageNumber, _currentCancellationSource.Token),
                _currentCancellationSource,
                _currentCancellationSource.Token).ContinueWith(QueryExecutionCompleted, uiScheduler);
        }

        private void EnsureExtensionCollection() {
            if (_extensions == null) {
                _extensions = new ObservableCollection<IVsExtension>();
            }
        }

        /// <summary>
        /// Called when user clicks on the Cancel button in the progress pane.
        /// </summary>
        private void CancelCurrentExtensionQuery() {
            Trace.WriteLine("Cancelling pending extensions query.");

            if (_currentCancellationSource != null) {
                _currentCancellationSource.Cancel();
                _loadingInProgress = false;
            }
        }

        /// <summary>
        /// This method executes on background thread.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We want to show error message inside the dialog, rather than blowing up VS.")]
        private LoadPageResult ExecuteAsync(int pageNumber, CancellationToken token) {
            token.ThrowIfCancellationRequested();

            if (_query == null) {
                IQueryable<IPackage> query = GetPackages();

                token.ThrowIfCancellationRequested();

                // Execute the total count query
                _totalCount = query.Count();

                token.ThrowIfCancellationRequested();

                // Apply the ordering then sort by id
                IQueryable<IPackage> orderedQuery = ApplyOrdering(query).ThenBy(p => p.Id);

                // Buffer 3 page sizes
                _query = orderedQuery.AsBufferedEnumerable(PageSize * 3);
            }

            IList<IPackage> packages = _query.DistinctLast(PackageEqualityComparer.Id, PackageComparer.Version)
                                             .Skip((pageNumber - 1) * PageSize)
                                             .Take(PageSize)
                                             .ToList();

            if (packages.Count < PageSize) {
                _totalCount = (pageNumber - 1) * PageSize + packages.Count;
            }

            token.ThrowIfCancellationRequested();

            return new LoadPageResult(packages, pageNumber, _totalCount);
        }

        private IOrderedQueryable<IPackage> ApplyOrdering(IQueryable<IPackage> query) {
            // If the default sort is null then fall back to rating
            if (Provider.CurrentSort == null) {
                return query.OrderByDescending(p => p.Rating);
            }

            // Order by the current descriptor
            return query.SortBy<IPackage>(Provider.CurrentSort, typeof(RecentPackage));
        }

        public IList<IVsSortDescriptor> GetSortDescriptors() {
            // Get the sort descriptor from the provider
            return Provider.SortDescriptors;
        }

        protected void ResetQuery() {
            _query = null;
        }

        public bool SortSelectionChanged(IVsSortDescriptor selectedDescriptor) {
            Provider.CurrentSort = selectedDescriptor as PackageSortDescriptor;

            if (Provider.CurrentSort != null) {
                // If we changed the sort order then invalidate the cache.
                ResetQuery();

                // Reload the first page since the sort order changed
                LoadPage(1);
                return true;
            }

            return false;
        }

        private void QueryExecutionCompleted(Task<LoadPageResult> task) {
            // If a task throws, the exception must be handled or the Exception
            // property must be accessed or the exception will tear down the process when finalized
            Exception exception = task.Exception;

            var cancellationSource = (CancellationTokenSource)task.AsyncState;
            if (cancellationSource != _currentCancellationSource) {
                return;
            }

            _loadingInProgress = false;

            // Only process the result if this node is still selected.
            if (IsSelected) {
                if (task.IsCanceled) {
                    HideProgressPane();
                }
                else if (task.IsFaulted) {
                    // show error message in the Message pane
                    ShowMessagePane((exception.InnerException ?? exception).Message);
                }
                else {
                    LoadPageResult result = task.Result;

                    IEnumerable<IPackage> packages = result.Packages;

                    _extensions.Clear();
                    foreach (IPackage package in packages) {
                        _extensions.Add(Provider.CreateExtension(package));
                    }

                    if (_extensions.Count > 0) {
                        _extensions[0].IsSelected = true;
                    }

                    int totalPages = (result.TotalCount + PageSize - 1) / PageSize;
                    int pageNumber = result.PageNumber;

                    TotalPages = Math.Max(1, totalPages);
                    CurrentPage = Math.Max(1, pageNumber);

                    HideProgressPane();
                }
            }

            if (QueryExecutionCallback != null) {
                QueryExecutionCallback();
            }
        }

        protected void OnNotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void SetProgressPane(IVsProgressPane progressPane) {
            ProgressPane = progressPane;
        }

        public void SetMessagePane(IVsMessagePane messagePane) {
            MessagePane = messagePane;
        }

        protected bool ShowProgressPane() {
            if (ProgressPane != null) {
                _progressPaneActive = true;
                return ProgressPane.Show(new CancelProgressCallback(CancelCurrentExtensionQuery), true);
            }
            else {
                return false;
            }
        }

        protected void HideProgressPane() {
            if (_progressPaneActive && ProgressPane != null) {
                ProgressPane.Close();
                _progressPaneActive = false;
            }
        }

        protected bool ShowMessagePane(string message) {
            if (MessagePane != null) {
                MessagePane.SetMessageThreadSafe(message);
                return MessagePane.Show();
            }
            else {
                return false;
            }
        }

        /// <summary>
        /// Called when this node is opened.
        /// </summary>
        internal void OnOpened() {
            if (!Provider.SuppressNextRefresh) {
                Provider.SelectedNode = this;
                if (Provider.RefreshOnNodeSelection && !this.IsSearchResultsNode) {
                    Refresh();
                }
            }
        }
    }
}
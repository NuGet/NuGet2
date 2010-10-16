using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {
    internal abstract class OnlinePackagesTreeBase : IVsExtensionsTreeNode, IVsPageDataSource, IVsProgressPaneConsumer, INotifyPropertyChanged, IVsMessagePaneConsumer {

        private delegate void ExecuteDelegate(IQueryable<IPackage> query, int pageNumber, int itemsPerPage, AsyncOperation async);

        // The number of extensions to show per page.
        private const int ItemsPerPage = 10;

        private IList<IVsExtension> _extensions = null;
        private IList<IVsExtensionsTreeNode> _nodes = null;
        private int _totalPages = 1, _currentPage = 1;
        private bool _progressPaneActive;
        private bool _isExpanded;
        private bool _isSelected;
        private object _activeQueryStateLock = new object();
        private bool _activeQueryStateCancelled;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> PageDataChanged;

        internal OnlinePackagesTreeBase(IVsExtensionsTreeNode parent, OnlinePackagesProvider provider) {
            Parent = parent;
            Provider = provider;
        }

        protected OnlinePackagesProvider Provider {
            get;
            set;
        }

        private IVsProgressPane ProgressPane {
            get;
            set;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance", 
            "CA1811:AvoidUncalledPrivateCode",
            Justification="We will need this property soon.")]
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
                _isSelected = value;
                OnNotifyPropertyChanged("IsSelected");
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
                _isExpanded = value;
                OnNotifyPropertyChanged("IsExpanded");
            }
        }

        /// <summary>
        /// List of templates at this node
        /// </summary>
        public IList<IVsExtension> Extensions {
            get {
                if (_extensions == null) {
                    _extensions = new ObservableCollection<IVsExtension>();
                    LoadPage(1);
                }
                else if (_activeQueryStateCancelled) {
                    LoadPage(CurrentPage);
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
                    FillNodes(_nodes);
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
                Trace.WriteLine("New Total Pages: " + value);
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

        public override string ToString() {
            return Name;
        }

        /// <summary>
        /// Helper function to raise property changed events
        /// </summary>
        /// <param name="info"></param>
        private void NotifyPropertyChanged() {
            if (PageDataChanged != null) {
                PageDataChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// Loads a specified page
        /// </summary>
        /// <param name="pageNumber"></param>
        public void LoadPage(int pageNumber) {
            Trace.WriteLine("Loading page " + pageNumber);
            if (pageNumber < 1) {
                return;
            }

            if (ProgressPane != null && ProgressPane.Show(new CancelProgressCallback(CancelCurrentExtensionQuery), true)) {
                _progressPaneActive = true;
            }

            if (_extensions != null) {
                _extensions.Clear();
            }
            GetExtensionsForPage(pageNumber);
        }

        public void SetProgressPane(IVsProgressPane progressPane) {
            ProgressPane = progressPane;
        }

        /// <summary>
        /// Cancels the current running query
        /// </summary>
        private void CancelCurrentExtensionQuery() {
            lock (_activeQueryStateLock) {
                Trace.WriteLine("Cancelling pending extensions query");
                _activeQueryStateCancelled = true;
            }

            if (_extensions != null) {
                _extensions.Clear();
            }
        }

        private void GetExtensionsForPage(object pageNumberObject) {
            if (!(pageNumberObject is int)) {
                return;
            }

            int pageNumber = (int)pageNumberObject;

            var query = GetQuery();

            lock (_activeQueryStateLock) {
                _activeQueryStateCancelled = false;
            }

            AsyncOperation async = AsyncOperationManager.CreateOperation(query);
            ExecuteDelegate worker = new ExecuteDelegate(ExecuteAsync);
            worker.BeginInvoke(query, pageNumber, ItemsPerPage, async, null, null);
        }

        // TODO: Investigate whether we should avoid catching general Exception
        // Temporary method for async
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ExecuteAsync(IQueryable<IPackage> query, int pageNumber, int itemsPerPage, AsyncOperation async) {
            IEnumerable<IPackage> packages = null;
            int totalCount = 0;

            try {
                // This should execute the query
                totalCount = query.Count();

                IQueryable<IPackage> pageQuery = query.Skip((pageNumber - 1) * itemsPerPage).Take(itemsPerPage);

                packages = pageQuery.ToList();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
            }
            finally {
                var e = new ExecuteCompletedEventArgs(null, false, null, packages, pageNumber, totalCount);
                async.PostOperationCompleted(new SendOrPostCallback(QueryExecutionCompleted), e);
            }
        }

        protected abstract IQueryable<IPackage> PreviewQuery(IQueryable<IPackage> query);

        protected abstract void FillNodes(IList<IVsExtensionsTreeNode> nodes);

        private IQueryable<IPackage> GetQuery() {
            var query = PreviewQuery(Provider.GetQuery());

            Trace.WriteLine("Query Created: " + query.ToString());
            return query;
        }

        private void QueryExecutionCompleted(object data) {
            var args = (ExecuteCompletedEventArgs)data;
            IEnumerable<IPackage> packages = args.Results;

            lock (_activeQueryStateLock) {
                _activeQueryStateCancelled = false;
            }

            int totalPages = 0;
            int pageNumber = 0;

            // Safe to access e.Results since we've already checked for 
            // e.Cancelled and e.Error
            foreach (IPackage package in packages) {
                _extensions.Add(new OnlinePackagesItem(Provider, package, false, null, 0, null));
            }

            if (_extensions.Count > 0) {
                _extensions[0].IsSelected = true;
            }

            totalPages = (args.TotalCount + ItemsPerPage - 1) / ItemsPerPage;
            pageNumber = args.PageNumber;

            TotalPages = (totalPages == 0) ? 1 : totalPages;
            CurrentPage = (totalPages == 0) ? 1 : pageNumber;

            if (_progressPaneActive && ProgressPane != null) {
                ProgressPane.Close();
            }
            _progressPaneActive = false;
        }

        protected void OnNotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void SetMessagePane(IVsMessagePane messagePane) {
            MessagePane = messagePane;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {
    internal abstract class OnlinePackagesTreeBase : IVsExtensionsTreeNode, IVsPageDataSource, IVsProgressPaneConsumer, INotifyPropertyChanged, IVsMessagePaneConsumer {
        /// <summary>
        /// A class used as a the userState object for ExecuteASync
        /// </summary>
        private class QueryState {
            public int PageNumber { get; set; }
        }

        //The number of extensions to show per page.
        private const int ItemsPerPage = 10;

        #region Private Members

        private IList<IVsExtension> m_Extensions = null;
        private IList<IVsExtensionsTreeNode> m_Nodes = null;
        private int m_TotalPages = 1, m_CurrentPage = 1;
        private Dispatcher m_CurrentDispatcher;
        private bool m_ProgressPaneActive = false;

        private QueryState m_ActiveQueryState;
        private object m_ActiveQueryStateLock = new object();
        private bool m_ActiveQueryStateCancelled = false;

        protected OnlinePackagesProvider Provider { get; set; }
        protected IPackageRepository Repository { get; set; }
        private IVsProgressPane ProgressPane { get; set; }
        private IVsMessagePane MessagePane { get; set; }

        #endregion

        internal OnlinePackagesTreeBase(IPackageRepository repository, IVsExtensionsTreeNode parent, OnlinePackagesProvider provider) {
            if (repository == null) throw new ArgumentNullException("repository");

            this.Parent = parent;
            this.Repository = repository;
            this.Provider = provider;
        }

        #region Public Properties

        /// <summary>
        /// Name of this node
        /// </summary>
        public abstract string Name { get; }

        public bool IsSearchResultsNode {
            get;
            set;
        }

        /// <summary>
        /// Select node (UI) property
        /// This property maps to TreeViewItem.IsSelected
        /// </summary>
        private bool _isSelected;
        public bool IsSelected {
            get { return _isSelected; }
            set { _isSelected = value; OnNotifyPropertyChanged("IsSelected"); }
        }

        /// <summary>
        /// Expand node (UI) property
        /// This property maps to TreeViewItem.IsExpanded
        /// </summary>
        private bool _isExpanded;
        public bool IsExpanded {
            get { return _isExpanded; }
            set { _isExpanded = value; OnNotifyPropertyChanged("IsExpanded"); }
        }

        /// <summary>
        /// List of templates at this node
        /// </summary>
        public IList<IVsExtension> Extensions {
            get {
                if (m_Extensions == null) {
                    m_Extensions = new ObservableCollection<IVsExtension>();
                    this.LoadPage(1);
                }
                else if (m_ActiveQueryStateCancelled) {
                    this.LoadPage(this.CurrentPage);
                }
                return m_Extensions;
            }
        }

        /// <summary>
        /// Children at this node
        /// </summary>
        public IList<IVsExtensionsTreeNode> Nodes {
            get {
                if (m_Nodes == null) {
                    m_Nodes = new ObservableCollection<IVsExtensionsTreeNode>();
                    this.FillNodes(m_Nodes);
                }
                return m_Nodes;
            }
        }
        /// <summary>
        /// Parent of this node
        /// </summary>
        public IVsExtensionsTreeNode Parent {
            get;
            private set;
        }

        #endregion

        public override string ToString() {
            return Name;
        }

        public event EventHandler<EventArgs> PageDataChanged;

        /// <summary>
        /// Helper function to raise property changed events
        /// </summary>
        /// <param name="info"></param>
        private void NotifyPropertyChanged() {
            if (PageDataChanged != null) {
                PageDataChanged(this, new EventArgs());
            }
        }

        #region IVsPageDataSource Members

        /// <summary>
        /// Loads a specified page
        /// </summary>
        /// <param name="pageNumber"></param>
        public void LoadPage(int pageNumber) {
            System.Diagnostics.Trace.WriteLine("Loading page " + pageNumber);
            if (pageNumber < 1) return;

            if (this.ProgressPane != null && this.ProgressPane.Show(new CancelProgressCallback(this.CancelCurrentExtensionQuery), true)) {
                this.m_ProgressPaneActive = true;
            }

            if (m_Extensions != null)
                m_Extensions.Clear();
            this.m_CurrentDispatcher = Dispatcher.CurrentDispatcher;
            this.GetExtensionsForPage(pageNumber);
        }

        public int TotalPages {
            get { return m_TotalPages; }

            internal set {
                System.Diagnostics.Trace.WriteLine("New Total Pages: " + value);
                m_TotalPages = value;

                this.NotifyPropertyChanged();
            }
        }

        public int CurrentPage {
            get { return m_CurrentPage; }

            internal set {
                m_CurrentPage = value;

                this.NotifyPropertyChanged();
            }
        }

        #endregion

        #region IVsProgressPaneConsumer Members

        public void SetProgressPane(IVsProgressPane progressPane) {
            this.ProgressPane = progressPane;
        }

        #endregion

        /// <summary>
        /// Cancels the current running query
        /// </summary>
        private void CancelCurrentExtensionQuery() {
            lock (m_ActiveQueryStateLock) {
                System.Diagnostics.Trace.WriteLine("Cancelling pending extensions query");
                m_ActiveQueryState = null;
                m_ActiveQueryStateCancelled = true;
            }
            if (m_Extensions != null) {
                m_Extensions.Clear();
            }
        }

        private void GetExtensionsForPage(object pageNumberObject) {
            if (!(pageNumberObject is int)) return;
            int pageNumber = (int)pageNumberObject;

            var query = GetQuery();

            lock (m_ActiveQueryStateLock) {
                //Create a new user state object that can be used for tracking this query.
                this.m_ActiveQueryState = new QueryState() {
                    PageNumber = pageNumber,
                };
                this.m_ActiveQueryStateCancelled = false;
            }

            AsyncOperation async = AsyncOperationManager.CreateOperation(query);
            ExecuteDelegate worker = new ExecuteDelegate(ExecuteAsync);
            worker.BeginInvoke(query, pageNumber, ItemsPerPage, async, null, null);
        }

        private delegate void ExecuteDelegate(IQueryable<IPackage> query, int pageNumber, int itemsPerPage, AsyncOperation async);

        // Temporary method for async
        void ExecuteAsync(IQueryable<IPackage> query, int pageNumber, int itemsPerPage, AsyncOperation async) {
            IEnumerable<IPackage> packages = null;
            int totalCount = 0;

            try {

                // This should execute the query
                totalCount = query.Count();

                IQueryable<IPackage> pageQuery = query.Skip((pageNumber - 1) * itemsPerPage).Take(itemsPerPage);

                packages = pageQuery;
                int resultCount = packages.Count();
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally {
                ExecuteCompletedEventArgs e = new ExecuteCompletedEventArgs(null, false, null, packages, pageNumber, totalCount);
                async.PostOperationCompleted(new SendOrPostCallback(QueryExecutionCompleted), e);
            }
        }


        protected abstract IQueryable<IPackage> PreviewQuery(IQueryable<IPackage> query);

        protected abstract void FillNodes(IList<IVsExtensionsTreeNode> nodes);

        private IQueryable<IPackage> GetQuery() {
            var query = this.PreviewQuery(this.Provider.GetQuery());

            System.Diagnostics.Trace.WriteLine("Query Created: " + query.ToString());
            return query;
        }

        void QueryExecutionCompleted(object data) {
            ExecuteCompletedEventArgs e = (ExecuteCompletedEventArgs)data;
            IEnumerable<IPackage> packages = e.Results;

            lock (m_ActiveQueryStateLock) {
                //Reset the currently active query.
                m_ActiveQueryState = null;
                m_ActiveQueryStateCancelled = false;
            }

            int totalPages = 0;
            int pageNumber = 0;

            //Safe to access e.Results since we've already checked for 
            //e.Cancelled and e.Error
            foreach (IPackage package in packages) {
                m_Extensions.Add(new OnlinePackagesItem(this.Provider, package, false, null, 0, null));
            }
            if (m_Extensions.Count > 0) {
                m_Extensions[0].IsSelected = true;
            }

            totalPages = (int)Math.Ceiling((double)e.TotalCount / (double)ItemsPerPage);
            pageNumber = e.PageNumber;

            this.TotalPages = (totalPages == 0) ? 1 : totalPages;
            this.CurrentPage = (totalPages == 0) ? 1 : pageNumber;

            if (this.m_ProgressPaneActive && this.ProgressPane != null) this.ProgressPane.Close();
            this.m_ProgressPaneActive = false;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnNotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region IVsMessagePaneConsumer Members

        public void SetMessagePane(IVsMessagePane messagePane) {
            this.MessagePane = messagePane;
        }

        #endregion
    }

    public class ExecuteCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        public ExecuteCompletedEventArgs(System.Exception exception, bool canceled, object userState, IEnumerable<IPackage> results, int pageNumber, int totalCount) :
            base(exception, canceled, userState) {
            this.results = results;
            this.pageNumber = pageNumber;
            this.totalCount = totalCount;
        }

        private IEnumerable<IPackage> results;
        public IEnumerable<IPackage> Results {
            get {
                this.RaiseExceptionIfNecessary();
                return results;
            }
        }

        private int totalCount;
        public int TotalCount {
            get {
                this.RaiseExceptionIfNecessary();
                return totalCount;
            }
        }

        private int pageNumber;
        public int PageNumber {
            get {
                this.RaiseExceptionIfNecessary();
                return pageNumber;
            }
        }
    }

}
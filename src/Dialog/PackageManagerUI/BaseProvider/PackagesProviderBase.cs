using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;
using NuGetConsole;

namespace NuGet.Dialog.Providers {
    /// <summary>
    /// Base class for all tree node types.
    /// </summary>
    internal abstract class PackagesProviderBase : VsExtensionsProvider, ILogger {

        private PackagesSearchNode _searchNode;
        private PackagesTreeNodeBase _lastSelectedNode;
        private readonly ResourceDictionary _resources;
        private readonly IProgressWindowOpener _progressWindowOpener;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly Lazy<IConsole> _outputConsole;

        private object _mediumIconDataTemplate;
        private object _detailViewDataTemplate;
        private IList<IVsSortDescriptor> _sortDescriptors;
        private Project _project;
        private readonly IProgressProvider _progressProvider;
        private CultureInfo _uiCulture, _culture;

        protected PackagesProviderBase(
            Project project,
            IProjectManager projectManager,
            ResourceDictionary resources,
            ProviderServices providerServices,
            IProgressProvider progressProvider) {

            if (projectManager == null) {
                throw new ArgumentNullException("projectManager");
            }

            if (project == null) {
                throw new ArgumentNullException("project");
            }

            if (resources == null) {
                throw new ArgumentNullException("resources");
            }

            if (providerServices == null) {
                throw new ArgumentNullException("providerServices");
            }

            _progressProvider = progressProvider;
            _resources = resources;
            _scriptExecutor = providerServices.ScriptExecutor;
            _progressWindowOpener = providerServices.ProgressWindow;
            _outputConsole = new Lazy<IConsole>(() => providerServices.OutputConsoleProvider.CreateOutputConsole(requirePowerShellHost: false));
            ProjectManager = projectManager;
            _project = project;
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

        public bool SuppressNextRefresh { get; private set; }

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
                new PackageSortDescriptor(Resources.Dialog_SortOption_MostDownloads, "DownloadCount", ListSortDirection.Descending),
                new PackageSortDescriptor(Resources.Dialog_SortOption_HighestRated, "Rating", ListSortDirection.Descending),
                new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortAscending), new[] { "Title", "Id" }, ListSortDirection.Ascending),
                new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortDescending), new[] { "Title", "Id" }, ListSortDirection.Descending)
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

                // When remove the search node, the dialog will automatically select the first node (All node)
                // Since we are going to restore the previously selected node anyway, we don't want the first node
                // to refresh. Hence suppress it here.
                SuppressNextRefresh = true;

                try {
                    // dispose any search results
                    RootNode.Nodes.Remove(_searchNode);
                }
                finally {
                    _searchNode = null;
                    SuppressNextRefresh = false;
                }

                if (_lastSelectedNode != null) {
                    // after search, we want to reset the original node to page 1 (Work Item #461) 
                    _lastSelectedNode.CurrentPage = 1;
                    SelectNode(_lastSelectedNode);
                }
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization",
            "CA1303:Do not pass literals as localized parameters",
            MessageId = "NuGet.Dialog.Providers.PackagesProviderBase.WriteLineToOutputWindow(System.String)",
            Justification = "No need to localize the --- strings"),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public virtual void Execute(PackageItem item) {
            if (OperationCoordinator.IsBusy) {
                return;
            }

            // disable all operations while this install is in progress
            OperationCoordinator.IsBusy = true;

            _progressProvider.ProgressAvailable += OnProgressAvailable;

            _uiCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            _culture = System.Threading.Thread.CurrentThread.CurrentCulture;

            var worker = new BackgroundWorker();
            worker.DoWork += OnRunWorkerDoWork;
            worker.RunWorkerCompleted += OnRunWorkerCompleted;
            worker.RunWorkerAsync(item);

            // write an introductory sentence before every operation starts to make the console easier to read
            string progressMessage = GetProgressMessage(item.PackageIdentity);
            WriteLineToOutputWindow("------- " + progressMessage + " -------");

            ShowProgressWindow();
        }

        private void OnProgressAvailable(object sender, ProgressEventArgs e) {
            _progressWindowOpener.ShowProgress(e.Operation, e.PercentComplete);
        }

        private void OnRunWorkerDoWork(object sender, DoWorkEventArgs e) {
            // make sure the new thread has the same cultures as the UI thread's cultures
            System.Threading.Thread.CurrentThread.CurrentUICulture = _uiCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = _culture;

            var item = (PackageItem)e.Argument;
            bool succeeded = ExecuteCore(item);
            e.Cancel = !succeeded;
            e.Result = item;
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            OperationCoordinator.IsBusy = false;

            _progressProvider.ProgressAvailable -= OnProgressAvailable;

            if (e.Error == null) {
                if (e.Cancelled) {
                    CloseProgressWindow();
                }
                else {
                    OnExecuteCompleted((PackageItem)e.Result);
                    _progressWindowOpener.SetCompleted(successful: true);
                }
            }
            else {
                // show error message in the progress window in case of error
                LogCore(LogMessageLevel.Error, (e.Error.InnerException ?? e.Error).Message);
                _progressWindowOpener.SetCompleted(successful: false);
            }

            // write a blank line into the output window to separate entries from different operations
            WriteLineToOutputWindow(new string('=', 30));
            WriteLineToOutputWindow();

            if (ExecuteCompletedCallback != null) {
                ExecuteCompletedCallback();
            }
        }

        protected void ShowProgressWindow() {
            _progressWindowOpener.Show(ProgressWindowTitle);
        }

        protected void HideProgressWindow() {
            _progressWindowOpener.Hide();
        }

        protected void CloseProgressWindow() {
            _progressWindowOpener.Close();
        }

        protected virtual void FillRootNodes() {
        }

        public abstract IVsExtension CreateExtension(IPackage package);

        public abstract bool CanExecute(PackageItem item);

        protected virtual string GetProgressMessage(IPackage package) {
            return package.ToString();
        }

        /// <summary>
        /// This method is called on background thread.
        /// </summary>
        /// <returns><c>true</c> if the method succeeded. <c>false</c> otherwise.</returns>
        protected virtual bool ExecuteCore(PackageItem item) {
            return true;
        }

        protected virtual void OnExecuteCompleted(PackageItem item) {
            // After every operation, just update the status of all packages in the current node.
            // Strictly speaking, this is not required; only affected packages need to be updated.
            // But doing so would require us to keep a Dictionary<IPackage, PackageItem> which is not worth it.
            if (SelectedNode != null) {
                foreach (PackageItem node in SelectedNode.Extensions) {
                    node.UpdateEnabledStatus();
                }
            }
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

        public void Log(MessageLevel level, string message, params object[] args) {
            var logLevel = (LogMessageLevel)level;
            LogCore(logLevel, message, args);
        }

        private void LogCore(LogMessageLevel level, string message, params object[] args) {
            string formattedMessage = String.Format(CultureInfo.CurrentCulture, message, args);

            // for the dialog we ignore debug messages
            if (_progressWindowOpener.IsOpen && level != LogMessageLevel.Debug) {
                _progressWindowOpener.AddMessage(level, formattedMessage);
            }

            WriteLineToOutputWindow(formattedMessage);
        }

        protected void WriteLineToOutputWindow(string message = "") {
            _outputConsole.Value.WriteLine(message);
        }

        protected void ShowProgress(string operation, int percentComplete) {
            if (_progressWindowOpener.IsOpen) {
                _progressWindowOpener.ShowProgress(operation, percentComplete);
            }
        }

        protected void RegisterPackageOperationEvents(IPackageManager packageManager) {
            packageManager.PackageInstalled += OnPackageInstalled;
            ProjectManager.PackageReferenceAdded += OnPackageReferenceAdded;
            ProjectManager.PackageReferenceRemoving += OnPackageReferenceRemoving;
        }

        protected void UnregisterPackageOperationEvents(IPackageManager packageManager) {
            packageManager.PackageInstalled -= OnPackageInstalled;
            ProjectManager.PackageReferenceAdded -= OnPackageReferenceAdded;
            ProjectManager.PackageReferenceRemoving -= OnPackageReferenceRemoving;
        }

        private void OnPackageInstalled(object sender, PackageOperationEventArgs e) {
            _scriptExecutor.ExecuteInitScript(e.InstallPath, e.Package, this);
        }

        private void OnPackageReferenceAdded(object sender, PackageOperationEventArgs e) {
            _scriptExecutor.ExecuteScript(e.InstallPath, PowerShellScripts.Install, e.Package, _project, this);
        }

        private void OnPackageReferenceRemoving(object sender, PackageOperationEventArgs e) {
            _scriptExecutor.ExecuteScript(e.InstallPath, PowerShellScripts.Uninstall, e.Package, _project, this);
        }
    }
}
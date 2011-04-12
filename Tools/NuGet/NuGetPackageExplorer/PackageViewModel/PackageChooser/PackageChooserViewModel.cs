using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kerr;
using NuGet;
using PackageExplorerViewModel.Types;
using NuGet.Utility;
using System.Net;
using PackageExplorerViewModel.PackageChooser;
using System.Windows;

namespace PackageExplorerViewModel {
    public class PackageChooserViewModel : ViewModelBase {
        private const int PageSize = 15;
        private IPackageRepository _packageRepository;
        private IQueryable<IPackage> _currentQuery;
        private string _currentSearch;
        private IMruPackageSourceManager _packageSourceManager;

        public PackageChooserViewModel(IMruPackageSourceManager packageSourceManager) {
            Packages = new ObservableCollection<IPackage>();
            NavigationCommand = new NavigateCommand(this);
            SortCommand = new SortCommand(this);
            SearchCommand = new SearchCommand(this);
            LoadedCommand = new LoadedCommand(this);
            ChangePackageSourceCommand = new ChangePackageSourceCommand(this);
           
            _packageSourceManager = packageSourceManager;
        }

        private string _sortColumn;

        public string SortColumn
        {
            get { return _sortColumn; }
            set {
                if (_sortColumn != value)
                {
                    _sortColumn = value;
                    OnPropertyChanged("SortColumn");
                }
            }
        }

        private ListSortDirection _sortDirection;

        public ListSortDirection SortDirection
        {
            get { return _sortDirection; }
            set {
                if (_sortDirection != value)
                {
                    _sortDirection = value;
                    OnPropertyChanged("SortDirection");
                }
            }
        }

        private int _sortCounter;

        public int SortCounter
        {
            get { return _sortCounter; }
            set {
                if (_sortCounter != value)
                {
                    _sortCounter = value;
                    OnPropertyChanged("SortCounter");
                }
            }
        }

        private bool _isEditable = true;

        public bool IsEditable {
            get {
                return _isEditable;
            }
            set {
                if (_isEditable != value) {
                    _isEditable = value;
                    OnPropertyChanged("IsEditable");
                }
            }
        }

        /// <summary>
        /// This method needs to be run on background thread so as not to block UI thread
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private IPackageRepository GetPackageRepository() {
            if (_packageRepository == null || _packageRepository.Source != PackageSource) {
                try {
                    ProxyType proxyType = HttpClientUtility.GetProxyType(PackageSource);
                    switch (proxyType)
                    {
                        case ProxyType.IntegratedAuth:
                            WebProxy ntmlProxy = HttpClientUtility.GetSystemProxy(PackageSource);
                            ntmlProxy.UseDefaultCredentials = true;
                            WebRequest.DefaultWebProxy = ntmlProxy;
                            break;
                        case ProxyType.BasicAuth:
                            WebProxy basicProxy = HttpClientUtility.GetSystemProxy(PackageSource);
                            string proxyHost = basicProxy.Address.Host;
                            // If the user opted to save the credentials then they will not be
                            // prompted again even though we are invoking the ShowDialog method
                            // what we need to do here is to try and detect if the credentials
                            // actually worked and if they did not then we have to ask the user once
                            // again and maybe remove the old credentials.
                            using (PromptForCredential dialog = new PromptForCredential())
                            {
                                dialog.TargetName = proxyHost;
                                dialog.Title = string.Format("Connect to: {0}", proxyHost);
                                dialog.GenericCredentials = true;
                                if (DialogResult.OK == dialog.ShowDialog())
                                {
                                    basicProxy.Credentials = new NetworkCredential(dialog.UserName, dialog.Password);
                                    WebRequest.DefaultWebProxy = basicProxy;
                                }
                            }
                            break;
                        case ProxyType.None:
                            // Before one would use the GlobalProxySelection.GetEmptyWebProxy() method
                            // to get an empty proxy since we don't want a proxy here but that class is now obsolete
                            // The recommended approach is to set the DefaultWebProxy to null as per the GlobalProxySelection
                            // documentation
                            // http://msdn.microsoft.com/en-us/library/system.net.globalproxyselection.aspx
                            WebRequest.DefaultWebProxy = null;
                            break;
                    }
                    _packageRepository = PackageRepositoryFactory.Default.CreateRepository(PackageSource);
                }
                catch (Exception) {
                    _packageRepository = null;
                }
            }
            return _packageRepository;
        }

        public ObservableCollection<string> PackageSources
        {
            get { return _packageSourceManager.PackageSources; }
        }

        public string PackageSource {
            get {
                return _packageSourceManager.ActivePackageSource; 
            }
            private set {
                _packageSourceManager.ActivePackageSource = value;
                OnPropertyChanged("PackageSource");
            }
        }

        private int _totalPackageCount;

        public int TotalPackageCount {
            get { return _totalPackageCount; }
            private set {
                if (_totalPackageCount != value) {
                    _totalPackageCount = value;
                    OnPropertyChanged("TotalPackageCount");
                    SortCommand.RaiseCanExecuteEvent();
                }
            }
        }

        private int _beginPackage;

        public int BeginPackage {
            get { return _beginPackage; }
            private set {
                if (_beginPackage != value) {
                    _beginPackage = value;
                    OnPropertyChanged("BeginPackage");
                }
            }
        }

        private int _endPackage;

        public int EndPackage {
            get { return _endPackage; }
            private set {
                if (_endPackage != value) {
                    _endPackage = value;
                    OnPropertyChanged("EndPackage");
                }
            }
        }

        private string _statusContent;

        public string StatusContent
        {
            get { return _statusContent; }
            set {
                if (_statusContent != value)
                {
                    _statusContent = value;
                    OnPropertyChanged("StatusContent");
                }
            }
        }

        public int TotalPage {
            get {
                return Math.Max(1, (TotalPackageCount + PageSize - 1) / PageSize);
            }
        }

        private int _currentPage;

        public int CurrentPage {
            get { return _currentPage; }
            private set {
                if (_currentPage != value) {
                    _currentPage = value;
                    OnPropertyChanged("CurrentPage");
                }
            }
        }

        public ObservableCollection<IPackage> Packages { get; private set; }

        public NavigateCommand NavigationCommand { get; private set; }

        public SortCommand SortCommand { get; private set; }

        public SearchCommand SearchCommand { get; private set; }

        public LoadedCommand LoadedCommand { get; private set; }

        public ChangePackageSourceCommand ChangePackageSourceCommand { get; private set; }

        public void LoadPage(int page) {
            Debug.Assert(_currentQuery != null);

            page = Math.Max(page, 0);
            page = Math.Min(page, TotalPage - 1);

            // load package
            var subQuery = _currentQuery.Skip(page * PageSize).Take(PageSize);

            var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            StatusContent = "Loading...";
            IsEditable = false;

            Task.Factory.StartNew<Tuple<IList<IPackage>, int>>(QueryPackages, subQuery).ContinueWith(
                result => {
                    if (result.IsFaulted) {
                        AggregateException exception = result.Exception;
                        StatusContent = (exception.InnerException ?? exception).Message;
                        ClearPackages();
                    }
                    else if (!result.IsCanceled) {
                        ShowPackages(result.Result.Item1, result.Result.Item2, page);
                        StatusContent = String.Empty;
                        // update sort column glyph
                        SortCounter++;
                    }

                    IsEditable = true;
                },
                uiScheduler);
        }

        private Tuple<IList<IPackage>, int> QueryPackages(object state) {
            var subQuery = (IQueryable<IPackage>)state;
            IList<IPackage> result = subQuery.ToList();

            int totalPackageCount = _currentQuery.Count();

            return Tuple.Create(result, totalPackageCount);
        }

        private void LoadPackages() {
            StatusContent = "Connecting to package source...";

            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew<IPackageRepository>(GetPackageRepository).ContinueWith(
                task => {
                    IPackageRepository repository = task.Result;
                    if (repository == null)
                    {
                        StatusContent = String.Empty;
                        ClearPackages();
                        return;
                    }

                    var query = repository.GetPackages();
                    if (!String.IsNullOrEmpty(_currentSearch)) {
                        query = query.Find(_currentSearch.Split(' '));
                    }

                    switch (SortColumn) {
                        case "Id":
                            query = SortDirection == ListSortDirection.Descending ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id);
                            break;

                        case "Authors":
                            query = SortDirection == ListSortDirection.Descending ? query.OrderByDescending(p => p.Authors) : query.OrderBy(p => p.Authors);
                            break;

                        case "VersionDownloadCount":
                            query = SortDirection == ListSortDirection.Descending ? query.OrderByDescending(p => p.VersionDownloadCount) : query.OrderBy(p => p.VersionDownloadCount);
                            break;

                        default:
                            query = query.OrderByDescending(p => p.VersionDownloadCount);
                            break;
                    }

                    _currentQuery = query;

                    // every time the search query changes, we reset to page 0
                    LoadPage(0);
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                uiScheduler
            );
        }

        public void Search(string searchTerm) {
            if (_currentSearch != searchTerm) {
                _currentSearch = searchTerm;
                LoadPackages();
            }
        }

        public void Sort(string column, ListSortDirection? direction = null) {
            if (SortColumn == column) {
                if (direction.HasValue) {
                    SortDirection = direction.Value;
                }
                else {
                    SortDirection = SortDirection == ListSortDirection.Ascending
                                        ? ListSortDirection.Descending
                                        : ListSortDirection.Ascending;
                }
            }
            else {
                SortColumn = column;
                SortDirection = direction ?? ListSortDirection.Ascending;
            }

            // trigger the dialog to update Sort glyph
            SortCounter++;

            LoadPackages();
        }

        public void ChangePackageSource(string source) {
            if (PackageSource != source || _currentQuery == null) {
                PackageSource = source;
                // add the new source to MRU list
                _packageSourceManager.NotifyPackageSourceAdded(source);

                LoadPackages();
            }
        }

        private void ClearPackages() {
            ShowPackages(Enumerable.Empty<IPackage>(), 0, 0);
        }

        private void ShowPackages(IEnumerable<IPackage> packages, int totalPackageCount, int page) {
            TotalPackageCount = totalPackageCount;

            CurrentPage = page;
            BeginPackage = Math.Min(page * PageSize + 1, TotalPackageCount);
            EndPackage = Math.Min((page + 1) * PageSize, TotalPackageCount);

            Packages.Clear();
            Packages.AddRange(packages);

            NavigationCommand.OnCanExecuteChanged();
        }
    }
}
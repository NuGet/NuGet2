using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {
    public class PackageChooserViewModel : ViewModelBase {
        private const int PageSize = 15;
        private DataServicePackageRepository _packageRepository;
        private IQueryable<PackageInfo> _currentQuery;
        private string _currentSearch;
        private string _redirectedlPackageSource;
        private IMruPackageSourceManager _packageSourceManager;
        private IProxyService _proxyService;
        private ICredentialProvider _credentialProvider;

        public PackageChooserViewModel(IMruPackageSourceManager packageSourceManager) {
            Packages = new ObservableCollection<PackageInfo>();
            NavigationCommand = new NavigateCommand(this);
            SortCommand = new RelayCommand<string>(Sort, column => TotalPackageCount > 0);
            SearchCommand = new RelayCommand<string>(Search);
            LoadedCommand = new RelayCommand(() => Sort("VersionDownloadCount", ListSortDirection.Descending));
            ChangePackageSourceCommand = new RelayCommand<string>(ChangePackageSource);
            _credentialProvider = new AutoDiscoverCredentialProvider();
            _proxyService = new ProxyService(_credentialProvider);

            _packageSourceManager = packageSourceManager;
        }

        private string _sortColumn;

        public string SortColumn {
            get { return _sortColumn; }
            set {
                if (_sortColumn != value) {
                    _sortColumn = value;
                    OnPropertyChanged("SortColumn");
                }
            }
        }

        private ListSortDirection _sortDirection;

        public ListSortDirection SortDirection {
            get { return _sortDirection; }
            set {
                if (_sortDirection != value) {
                    _sortDirection = value;
                    OnPropertyChanged("SortDirection");
                }
            }
        }

        private int _sortCounter;

        public int SortCounter {
            get { return _sortCounter; }
            set {
                if (_sortCounter != value) {
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
        private DataServicePackageRepository GetPackageRepository() {
            if (_packageRepository == null || _packageRepository.Source != _redirectedlPackageSource) {
                try {
                    Uri packageUri = new Uri(PackageSource);
                    IWebProxy packageSourceProxy = _proxyService.GetProxy(packageUri);
                    IHttpClient packageSourceClient = new RedirectedHttpClient(packageUri, packageSourceProxy);
                    _packageRepository = new DataServicePackageRepository(packageSourceClient);
                    _redirectedlPackageSource = _packageRepository.Source;
                }
                catch (Exception) {
                    _packageRepository = null;
                }
            }
            return _packageRepository;
        }

        public ObservableCollection<string> PackageSources {
            get { return _packageSourceManager.PackageSources; }
        }

        public string PackageSource {
            get {
                return _packageSourceManager.ActivePackageSource;
            }
            private set {
                _packageSourceManager.ActivePackageSource = value;
                _redirectedlPackageSource = null;
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

        public string StatusContent {
            get { return _statusContent; }
            set {
                if (_statusContent != value) {
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

        public ObservableCollection<PackageInfo> Packages { get; private set; }

        public NavigateCommand NavigationCommand { get; private set; }
        public ICommand SortCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand LoadedCommand { get; private set; }
        public ICommand ChangePackageSourceCommand { get; private set; }

        internal void LoadPage(int page) {
            Debug.Assert(_currentQuery != null);

            page = Math.Max(page, 0);
            page = Math.Min(page, TotalPage - 1);

            // load package
            var subQuery = _currentQuery.Skip(page * PageSize).Take(PageSize);

            var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            StatusContent = "Loading...";
            IsEditable = false;

            Task.Factory.StartNew<Tuple<IList<PackageInfo>, int>>(QueryPackages, subQuery).ContinueWith(
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

        private Tuple<IList<PackageInfo>, int> QueryPackages(object state) {
            var subQuery = (IQueryable<PackageInfo>)state;
            IList<PackageInfo> result = subQuery.ToList();
            foreach (PackageInfo entity in result) {
                entity.DownloadUrl = GetPackageRepository().GetReadStreamUri(entity);
            }

            int totalPackageCount = _currentQuery.Count();

            return Tuple.Create(result, totalPackageCount);
        }

        private void LoadPackages() {
            StatusContent = "Connecting to package source...";

            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew<DataServicePackageRepository>(GetPackageRepository).ContinueWith(
                task => {
                    DataServicePackageRepository repository = task.Result;
                    if (repository == null) {
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

                        case "Rating":
                            query = SortDirection == ListSortDirection.Descending ? query.OrderByDescending(p => p.VersionRating) : query.OrderBy(p => p.VersionRating);
                            break;

                        default:
                            query = query.OrderByDescending(p => p.VersionDownloadCount);
                            break;
                    }

                    _currentQuery = query.Select(p => new PackageInfo { 
                        Id = p.Id, 
                        Version = p.Version, 
                        Authors = p.Authors, 
                        VersionRating = p.VersionRating, 
                        VersionDownloadCount = p.VersionDownloadCount,
                        PackageHash = p.PackageHash
                    });

                    // every time the search query changes, we reset to page 0
                    LoadPage(0);
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                uiScheduler
            );
        }

        private void Search(string searchTerm) {
            if (_currentSearch != searchTerm) {
                _currentSearch = searchTerm;
                LoadPackages();
            }
        }

        private void Sort(string column) {
            Sort(column, null);
        }

        private void Sort(string column, ListSortDirection? direction) {
            if (column == "Version") {
                // we can't sort Version
                return;
            }

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

        private void ChangePackageSource(string source) {
            if (PackageSource != source || _currentQuery == null) {
                PackageSource = source;
                // add the new source to MRU list
                _packageSourceManager.NotifyPackageSourceAdded(source);

                LoadPackages();
            }
        }

        private void ClearPackages() {
            ShowPackages(Enumerable.Empty<PackageInfo>(), 0, 0);
        }

        private void ShowPackages(IEnumerable<PackageInfo> packages, int totalPackageCount, int page) {
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
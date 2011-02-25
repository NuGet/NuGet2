using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NuGet;

namespace PackageExplorerViewModel {
    public class PackageChooserViewModel : ViewModelBase {
        private const string NuGetFeed = "https://go.microsoft.com/fwlink/?LinkID=206669";
        private const int PageSize = 15;
        private IPackageRepository _packageRepository;
        private IQueryable<IPackage> _currentQuery;
        private string _currentSearch;

        public PackageChooserViewModel() {
            Packages = new ObservableCollection<IPackage>();
            NavigationCommand = new NavigateCommand(this);
            SortCommand = new SortCommand(this);
            SearchCommand = new SearchCommand(this);
            LoadedCommand = new LoadedCommand(this);
        }

        public string CurrentSortColumn {
            get;
            set;
        }

        public ListSortDirection SortDirection {
            get;
            set;
        }

        private IPackageRepository PackageRepository {
            get {
                if (_packageRepository == null) {
                    _packageRepository = PackageRepositoryFactory.Default.CreateRepository(NuGetFeed);
                    
                }
                return _packageRepository;
            }
        }

        private int _totalPackageCount;

        public int TotalPackageCount {
            get { return _totalPackageCount; }
            private set {
                if (_totalPackageCount != value) {
                    _totalPackageCount = value;
                    RaisePropertyChangeEvent("TotalPackageCount");
                }
            }
        }

        private int _beginPackage;

        public int BeginPackage {
            get { return _beginPackage; }
            private set {
                if (_beginPackage != value) {
                    _beginPackage = value;
                    RaisePropertyChangeEvent("BeginPackage");
                }
            }
        }

        private int _endPackage;

        public int EndPackage {
            get { return _endPackage; }
            private set {
                if (_endPackage != value) {
                    _endPackage = value;
                    RaisePropertyChangeEvent("EndPackage");
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
                    RaisePropertyChangeEvent("StatusContent");
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
                    RaisePropertyChangeEvent("CurrentPage");
                }
            }
        }

        public ObservableCollection<IPackage> Packages { get; private set; }

        public NavigateCommand NavigationCommand { get; private set; }

        public SortCommand SortCommand { get; private set; }

        public SearchCommand SearchCommand { get; private set; }

        public LoadedCommand LoadedCommand { get; set; }

        public void LoadPage(int page) {
            Debug.Assert(_currentQuery != null);

            page = Math.Max(page, 0);
            page = Math.Min(page, TotalPage - 1);

            // load package
            var subQuery = _currentQuery.Skip(page * PageSize).Take(PageSize);

            var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            StatusContent = "Loading...";

            Task.Factory.StartNew<Tuple<IList<IPackage>, int>>(QueryPackages, subQuery).ContinueWith(
                result => {
                    if (result.IsCompleted) {
                        TotalPackageCount = result.Result.Item2;
                        SetPackages(result.Result.Item1);

                        CurrentPage = page;
                        BeginPackage = Math.Min(page*PageSize + 1, TotalPackageCount);
                        EndPackage = Math.Min((page + 1)*PageSize, TotalPackageCount);

                        NavigationCommand.RaiseCanExecuteChangedEvent();
                    }

                    StatusContent = String.Empty;
                },
                uiScheduler);
        }

        private Tuple<IList<IPackage>, int> QueryPackages(object state) {
            var subQuery = (IQueryable<IPackage>)state;
            IList<IPackage> result = subQuery.ToList();

            int totalPackageCount = _currentQuery.Count();

            return Tuple.Create(result, totalPackageCount);
        }

        public void LoadPackages() {
            var query = PackageRepository.GetPackages();
            if (!String.IsNullOrEmpty(_currentSearch)) {
                query = query.Find(_currentSearch.Split(' '));
            }

            switch (CurrentSortColumn) {
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
        }

        private void SetPackages(IEnumerable<IPackage> packages) {
            Packages.Clear();
            Packages.AddRange(packages);
        }

        public void Search(string searchTerm) {
            if (_currentSearch != searchTerm) {
                _currentSearch = searchTerm;
                LoadPackages();
            }
        }

        public void Sort(string column, ListSortDirection? direction = null) {
            if (CurrentSortColumn == column) {
                if (direction.HasValue) {
                    SortDirection = direction.Value;
                }
                else {
                    SortDirection = SortDirection == ListSortDirection.Ascending
                                        ? ListSortDirection.Descending
                                        : ListSortDirection.Ascending;
                }
                RaisePropertyChangeEvent("SortDirection");
            }
            else {
                CurrentSortColumn = column;
                SortDirection = direction ?? ListSortDirection.Ascending;
                RaisePropertyChangeEvent("CurrentSortColumn");
            }
            
            LoadPackages();
        }
    }
}
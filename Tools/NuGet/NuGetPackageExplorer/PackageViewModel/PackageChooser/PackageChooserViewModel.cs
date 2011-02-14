using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NuGet;

namespace PackageExplorerViewModel {
    public class PackageChooserViewModel : ViewModelBase {
        private const string NuGetFeed = "https://go.microsoft.com/fwlink/?LinkID=206669";
        private const int PageSize = 15;
        private IPackageRepository _packageRepository;

        public PackageChooserViewModel() {
            // we assume the number of packages won't change during the dialog session
            TotalPackageCount = PackageRepository.GetPackages().Count();
            LoadPage(0);
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

        private ObservableCollection<IPackage> _packages;
        public ObservableCollection<IPackage> Packages {
            get {
                if (_packages == null) {
                    _packages = new ObservableCollection<IPackage>();
                }
                return _packages;
            }
        }

        private NavigateCommand _navigationCommand;

        public NavigateCommand NavigationCommand {
            get {
                if (_navigationCommand == null) {
                    _navigationCommand = new NavigateCommand(this);
                }

                return _navigationCommand;
            }
        }

        public void LoadPage(int page) {
            page = Math.Max(page, 0);
            page = Math.Min(page, TotalPage - 1);

            // load package
            var packages = PackageRepository.GetPackages().Skip(page*PageSize).Take(PageSize);

            SetPackages(packages);
            CurrentPage = page;
            BeginPackage = page * PageSize + 1;
            EndPackage = (page + 1)*PageSize;
        }

        private void SetPackages(IEnumerable<IPackage> packages) {
            Packages.Clear();
            Packages.AddRange(packages);
        }

    }
}
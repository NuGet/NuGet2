using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.WebMatrix.Core.SQM;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Utility;
using NuGet;
using NuGet.WebMatrix.Data;

namespace NuGet.WebMatrix
{
    internal class NuGetViewModel : NotifyPropertyChanged
    {
        private object _selectedItem;
        private PackageViewModel _selectedPackage;
        private PackageViewModelAction _packageAction;
        private bool _loading;
        private string _loadingMessage;
        private IListViewFilter _selectedFilter;
        private string _selectedFilterName = Resources.Filter_All;
        private bool _isDetailsPaneVisible;
        private bool _isLicencePageVisible;
        private bool _isUninstallPageVisible;
        private ObservableCollection<IListViewFilter> _filters;
        private object _selectedFeedSourceItem;
        private Task _primaryTask;
        private readonly Func<Uri, string, INuGetPackageManager> _packageManagerCreator;
        private string _message;
        private string _searchString;
        private long _searchCount;
        private NuGetModel _nuGetModel;
        private bool _includePrerelease = false;
        private IPreferences _preferences = null;

        private const string PrereleaseFilterKey = "Prerelease";

        /// <summary>
        /// File system location to install packages, defaults to current site directory
        /// if not set
        /// </summary>
        private string _destination;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NuGetViewModel"/> class.
        /// </summary>
        public NuGetViewModel(
            INuGetGalleryDescriptor descriptor,
            IWebMatrixHost host,
            PackageSourcesViewModel packageSourcesViewModel, 
            Func<Uri, string, INuGetPackageManager> packageManagerCreator, 
            string destination,
            TaskScheduler scheduler)
        {
            this.Descriptor = descriptor;
            this.Host = host;
            this.Scheduler = scheduler;

            _packageManagerCreator = packageManagerCreator;
            _destination = destination;
            _preferences = this.Host.GetExtensionSpecificPreferences(this.Descriptor.PreferencesStore);
            _selectedPrereleaseFilter = LoadPrereleaseFilter();
            _includePrerelease = (_selectedPrereleaseFilter == Resources.Prerelease_Filter_IncludePrerelease);
            PackagesToDisplayForUpdateAll = new List<PackageViewModel>();

            this.ShowDetailsPageCommand = new RelayCommand(this.ShowDetailsPage, this.CanShowDetailsPage);
            this.ShowLicensePageCommand = new RelayCommand(this.ShowLicensePage, this.CanShowLicensePage);
            this.ShowUninstallPageCommand = new RelayCommand(this.ShowUninstallPage, this.CanShowUninstallPage);

            this.ShowListCommand = new RelayCommand(this.ShowList, this.CanShowList);

            this.InstallCommand = new RelayCommand(this.Install, this.CanInstall);
            this.UpdateCommand = new RelayCommand(this.Update, this.CanUpdate);
            this.UninstallCommand = new RelayCommand(this.Uninstall, this.CanUninstall);

            this.UpdateAllCommand = new RelayCommand(this.UpdateAll, this.CanUpdateAll);
            this.ShowLicensePageForAllCommand = new RelayCommand(this.ShowLicensePageForAll, this.CanShowLicensePageForAll);

            this.DisableCommand = new RelayCommand(this.Disable, this.CanDisable);
            this.EnableCommand = new RelayCommand(this.Enable, this.CanEnable);

            this.DefaultActionCommand = new RelayCommand(this.DefaultAction, this.CanDefaultAction);

            // Initialize the Package Sources
            InitializePackageSources(packageSourcesViewModel);
        }

        public INuGetGalleryDescriptor Descriptor
        {
            get;
            private set;
        }

        public IWebMatrixHost Host
        {
            get;
            private set;
        }

        public TaskScheduler Scheduler
        {
            get;
            private set;
        }

        private void InitializePackageSources(PackageSourcesViewModel packageSourcesViewModel)
        {
            // Set the loading message and set Loading to true
            Loading = true;
            LoadingMessage = this.Descriptor.LoadingMessage;

            // Setup the PackageSourceViewModel
            this.PackageSourcesViewModel = packageSourcesViewModel;
            this.SelectedFeedSourceItem = this.PackageSourcesViewModel.ActiveFeedSource;
        }

        private void BeginUpdateModel(FeedSource source, bool includePrerelease)
        {
            if (source == null)
            {
                Debug.Fail("We should always have a feed source selected");
                return;
            }

            // Set the loading message and set Loading to true
            Loading = true;
            LoadingMessage = this.Descriptor.LoadingMessage;

            _nuGetModel = NuGetModel.GetModel(
                this.Descriptor, 
                this.Host, 
                source, 
                _destination ?? this.Host.WebSite.Path, 
                _packageManagerCreator,
                this.Scheduler,
                includePrerelease);

            this.Filters = _nuGetModel.FilterManager.Filters;

            // we're using attached to parent here to prevent a race condition in tests for install/uninstall/update actions
            // we don't want the task set by 'EndInstall' to complete until after we've reloaded
            var updateModelTask = Task.Factory.StartNew(() => _nuGetModel.FilterManager.UpdateFilters(), CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskScheduler.Default);
            _primaryTask = updateModelTask.ContinueWith(EndUpdateModel, CancellationToken.None, TaskContinuationOptions.AttachedToParent, this.Scheduler);
        }

        // this is here for tests, we want to make sure loading is complete
        internal void WaitUntilComplete()
        {
            if (!_primaryTask.IsCompleted)
            {
                _primaryTask.Wait();
            }

            Debug.Assert(!this.Loading, "The Loading property must be set to false");
        }

        private void EndUpdateModel(Task task)
        {
            try
            {
                Exception exception = task.Exception;
                if (task.IsCanceled)
                {
                    // If the task has been cancelled, simply return
                    return;
                }
                else if (task.IsFaulted)
                {
                    this.ShowError(task.Exception.Flatten().InnerException);
                }

                IListViewFilter filter = this.Filters.Where(p => p.Name == _selectedFilterName).FirstOrDefault();

                SelectedFilter = filter ?? this.Filters.FirstOrDefault();
            }
            finally
            {
                Loading = false;
                LoadingMessage = null;
            }
        }

        private void SetDetailsPaneVisibility(object state)
        {
            bool value;
            if (bool.TryParse(state as string, out value))
            {
                IsDetailsPaneVisible = value;
            }
            else
            {
                IsDetailsPaneVisible = false;
            }
        }

        private void ExecuteToggleEnableAction()
        {
            Debug.Assert(SelectedPackage != null, "Must have a selected package.");
            var packageViewModel = (PackageViewModel)SelectedPackage;

            Debug.Assert(
                packageViewModel.SupportsEnableDisable,
                "This should not be called if a package can't be enabled/disabled");

            packageViewModel.IsEnabled = !packageViewModel.IsEnabled;
            OnPropertyChanged("ToggleEnableActionLabel");

            // refresh the UI since the set of disabled packages changed
            this.BeginUpdateModel(this.SelectedFeedSource, _includePrerelease);

            // Log the result of the enable toggle
            var telemetry = WebMatrixTelemetryServiceProvider.GetTelemetryService();
            if (telemetry != null)
            {
                string appId = this.Host.WebSite.ApplicationIdentifier;
                if (packageViewModel.IsEnabled)
                {
                    telemetry.LogPackageEnabled(this.Descriptor.GalleryId, packageViewModel.Id, appId, null, IsDetailsPaneVisible, false, !SelectedFeedSource.IsBuiltIn);
                }
                else
                {
                    telemetry.LogPackageDisabled(this.Descriptor.GalleryId, packageViewModel.Id, appId, null, IsDetailsPaneVisible, false, !SelectedFeedSource.IsBuiltIn);
                }
            }
        }

        public bool IsSearching
        {
            get
            {
                return Interlocked.Read(ref _searchCount) > 0;
            }
        }

        public string SearchingMessage
        {
            get
            {
                return Resources.String_Searching;
            }
        }

        public bool Loading
        {
            get
            {
                return _loading;
            }

            private set
            {
                _loading = value;
                OnPropertyChanged("Loading");

                // IIS-OOB #34652 -- Push a notification that the loading-state has changed
                // which will affect button state
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsUpdatingAll
        {
            get
            {
                return PackageAction == PackageViewModelAction.UpdateAll;
            }
        }

        public bool NotUpdatingAll
        {
            get
            {
                return !IsUpdatingAll;
            }
        }

        public string LoadingMessage
        {
            get
            {
                return _loadingMessage;
            }

            private set
            {
                _loadingMessage = value;
                OnPropertyChanged("LoadingMessage");
            }
        }

        public PackageViewModelAction PackageAction
        {
            get
            {
                return _packageAction;
            }

            private set
            {
                if (_packageAction != value)
                {
                    _packageAction = value;
                    OnPropertyChanged("PackageAction");
                }
            }
        }

        public PackageSourcesViewModel PackageSourcesViewModel
        {
            get;
            private set;
        }

        private bool _ShouldShowPrereleaseFilter;

        internal bool ShouldShowPrereleaseFilter
        {
            get
            {
                return SelectedFilter != null ? SelectedFilter.SupportsPrereleaseFilter && _ShouldShowPrereleaseFilter : _ShouldShowPrereleaseFilter;
            }

            set
            {
                if (_ShouldShowPrereleaseFilter != value)
                {
                    _ShouldShowPrereleaseFilter = value;
                    if (SelectedFilter == null)
                    {
                        ShowPrereleaseFilter = value ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else
                    {
                        ShowPrereleaseFilter = value && SelectedFilter.SupportsPrereleaseFilter ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        internal bool ShouldShowFeedSource
        {
            // Currently, we use SupportsPrereleaseFilter itself to determine
            // whether or not to show the FeedSource
            // Note that SupportsPrereleaseFilter is not equivalent to ShouldShowPrereleaseFilter
            get
            {
                return SelectedFilter != null ? SelectedFilter.SupportsPrereleaseFilter : true;
            }
        }

        private Visibility _showPrereleaseFilter = Visibility.Collapsed;

        public Visibility ShowPrereleaseFilter
        {
            get
            {
                return _showPrereleaseFilter;
            }

            set
            {
                if (_showPrereleaseFilter != value)
                {
                    _showPrereleaseFilter = value;
                    OnPropertyChanged("ShowPrereleaseFilter");
                }
            }
        }

        private string _selectedPrereleaseFilter;

        public string SelectedPrereleaseFilter
        {
            get
            {
                return _selectedPrereleaseFilter;
            }

            set
            {
                if (_selectedPrereleaseFilter != value)
                {
                    _selectedPrereleaseFilter = value;
                    SavePrereleaseFilter(value);
                    _includePrerelease = (_selectedPrereleaseFilter == Resources.Prerelease_Filter_IncludePrerelease);

                    // refresh the UI
                    this.BeginUpdateModel(this.SelectedFeedSource, _includePrerelease);

                    OnPropertyChanged("SelectedPrereleaseFilter");
                }
            }
        }

        private Visibility _showFeedSourceComboBox = Visibility.Visible;

        public Visibility ShowFeedSourceComboBox
        {
            get
            {
                return _showFeedSourceComboBox;
            }

            set
            {
                if (_showFeedSourceComboBox != value)
                {
                    _showFeedSourceComboBox = value;
                    OnPropertyChanged("ShowFeedSourceComboBox");
                }
            }
        }

        public string SearchString
        {
            get
            {
                return _searchString;
            }

            set
            {
                if (_searchString != value)
                {
                    _searchString = value;
                    this.OnPropertyChanged("SearchString");

                    this.BeginSearch();
                }
            }
        }

        public FeedSource SelectedFeedSource
        {
            get
            {
                return this.SelectedFeedSourceItem as FeedSource;
            }
        }

        public object SelectedFeedSourceItem
        {
            get
            {
                return _selectedFeedSourceItem;
            }

            set
            {
                if (_selectedFeedSourceItem != value)
                {
                    FeedSource source = value as FeedSource;
                    if (source != null)
                    {
                        // only store the change if it's actually a feed source
                        _selectedFeedSourceItem = value;

                        // if the item is a feed source, update the UI
                        OnSelectedFeedSourceChanged(source);
                    }
                    else
                    {
                        // if the item is not a feed source, show the source manager
                        SynchronizationContext.Current.Post((state) =>
                        {
                            var dialog = new SourceManagerView();
                            dialog.DataContext = this.PackageSourcesViewModel;
                            this.Host.ShowDialog(null, dialog);

                            // the feed source can change while the dialog is open, the user
                            // could delete a feed
                            this.SelectedFeedSourceItem = this.PackageSourcesViewModel.ActiveFeedSource;
                        }, null);
                    }
                }

                OnPropertyChanged("SelectedFeedSource");
                OnPropertyChanged("SelectedFeedSourceItem");
            }
        }

        private void OnSelectedFeedSourceChanged(FeedSource value)
        {
            try
            {
                // hide the details pane, the user selected a different feed
                this.IsDetailsPaneVisible = false;

                BeginUpdateModel(value, _includePrerelease);

                // this will save the change to the preferences store
                this.PackageSourcesViewModel.ActiveFeedSource = value;
            }
            catch (Exception exception)
            {
                this.ShowError(exception);
            }
            finally
            {
                OnPropertyChanged("SelectedFeedSource");
                OnPropertyChanged("SelectedFeedSourceItem");
            }
        }

        public bool IsDetailsPaneVisible
        {
            get
            {
                return _isDetailsPaneVisible;
            }

            set
            {
                _isDetailsPaneVisible = value;
                OnPropertyChanged("IsDetailsPaneVisible");
                UpdateMessage();
            }
        }

        /// <summary>
        /// Determines whether or not the page related to the PerformAction should
        /// be displayed
        /// </summary>
        public bool IsLicensePageVisible
        {
            get
            {
                return _isLicencePageVisible;
            }

            set
            {
                _isLicencePageVisible = value;
                OnPropertyChanged("IsLicensePageVisible");
                OnPropertyChanged("IsUpdatingAll");
                OnPropertyChanged("NotUpdatingAll");

            }
        }

        /// <summary>
        /// Determines whether or not the page related to uninstalling should
        /// be displayed
        /// </summary>
        public bool IsUninstallPageVisible
        {
            get
            {
                return _isUninstallPageVisible;
            }

            set
            {
                _isUninstallPageVisible = value;
                OnPropertyChanged("IsUninstallPageVisible");
            }
        }

        public bool ShowUpdateAll
        {
            get
            {
                IListViewFilter updatesFilter = this.Filters.Where(p => p.Name == Resources.Filter_Updated).FirstOrDefault();
                return (SelectedFilter != null && SelectedFilter.Name == Resources.Filter_Updated) && (updatesFilter != null ? (updatesFilter.Count > 1) : false);
            }
        }

        private void UpdateMessage()
        {
            string message = null;

            if (IsDetailsPaneVisible)
            {
                PackageViewModel selectedPackage = this.SelectedPackage;
                if (this.SelectedPackage != null)
                {
                    message = this.SelectedPackage.Message;
                }
            }
                
            Message = message;
        }

        public string Message
        {
            get
            {
                return _message;
            }

            set
            {
                _message = value;
                OnPropertyChanged("Message");
            }
        }

        public string SearchMessage
        {
            get
            {
                return Resources.String_Searching;
            }
        }

        public ObservableCollection<IListViewFilter> Filters
        {
            get
            {
                return _filters;
            }

            private set
            {
                _filters = value;
                OnPropertyChanged("Filters");
            }
        }

        public IListViewFilter SelectedFilter
        {
            get
            {
                return _selectedFilter;
            }

            set
            {
                _selectedFilter = value;
                if (value != null)
                {
                    _selectedFilterName = value.Name;
                }

                // hide the details pane, the user selected a different category
                this.IsDetailsPaneVisible = false;

                // IISOOB #37146 - if you have a selected item while switching categories
                // WPF will scan the new list of data trying to find the currently selected item
                // this can cause VirtualizingList to fetch all data at once -- this creates too
                // tasks and can cause hangs. The workaround for now is deselect before changing 
                // lists -- this will prevent all of the data from being scanned.
                this.SelectedItem = null;

                OnPropertyChanged("SelectedFilter");
                this.BeginSearch();
            }
        }

        public List<PackageViewModel> PackagesToDisplayForUpdateAll
        {
            get;
            private set;
        }

        public object SelectedItem
        {
            get
            {
                return _selectedItem;
            }

            set
            {
                if (_selectedItem != value)
                {
                    var oldValue = _selectedItem;
                    _selectedItem = value;

                    OnPropertyChanged("SelectedItem");
                    OnSelectedItemChanged(oldValue, _selectedItem);
                }
            }
        }

        public PackageViewModel SelectedPackage
        {
            get
            {
                return _selectedPackage;
            }

            private set
            {
                if (_selectedPackage != value)
                {
                    _selectedPackage = value;

                    OnPropertyChanged("SelectedPackage");

                    // fire the canexecute to update the button
                    // when the change notification arrives here via a virtualizinglistentry,
                    // sometimes the change won't be picked up
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private void OnSelectedItemChanged(object oldValue, object newValue)
        {
            var oldVirtualizingListEntry = oldValue as VirtualizingListEntry;
            if (oldVirtualizingListEntry != null)
            {
                oldVirtualizingListEntry.PropertyChanged -= this.SelectedItem_PropertyChanged;
            }

            var virtualizingListViewEntry = newValue as VirtualizingListEntry;
            var listViewItemWrapper = newValue as ListViewItemWrapper;

            if (virtualizingListViewEntry != null)
            {
                virtualizingListViewEntry.PropertyChanged += this.SelectedItem_PropertyChanged;
                SelectedPackage = virtualizingListViewEntry.Item as PackageViewModel;
            }
            else if (listViewItemWrapper != null)
            {
                SelectedPackage = listViewItemWrapper.Item as PackageViewModel;
            }
            else
            {
                SelectedPackage = null;
            }
        }

        private void BeginSearch()
        {
            var virtualizingListViewFilter = this.SelectedFilter as VirtualizingListViewFilter;
            if (this.SelectedFilter == null)
            {
                return;
            }
            else if (this.SelectedFilter is ListViewFilter)
            {
                // search on the UI thread for ListViewFilter, the collection is all
                // in memory
                this.SelectedFilter.FilterItemsForDisplay(this.SearchString);
            }
            else if (virtualizingListViewFilter != null)
            {
                if (String.IsNullOrWhiteSpace(this.SearchString))
                {
                    // this is a 'clear' or an initial load, this data is cached, so it's safe
                    // to do on the UI thread
                    this.SelectedFilter.FilterItemsForDisplay(this.SearchString);
                }
                else
                {
                    Interlocked.Increment(ref _searchCount);
                    this.OnPropertyChanged("IsSearching");

                    virtualizingListViewFilter
                        .BeginSearch(this.SearchString, this.Scheduler)
                        .ContinueWith(this.CompleteSearch);
                }
            }
        }

        private void CompleteSearch(Task searchTask)
        {
            Interlocked.Decrement(ref _searchCount);
            this.OnPropertyChanged("IsSearching");
        }

        private void SelectedItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.EqualsCaseSensitive("Item"))
            {
                Task.Factory.StartNew(
                    () => this.OnSelectedItemChanged(_selectedItem, _selectedItem), 
                    CancellationToken.None, 
                    TaskCreationOptions.None,
                    this.Scheduler);
            }
        }

        private void ShowError(Exception exception)
        {
            this.Host.ShowExceptionMessage(Resources.String_Error, Resources.String_ErrorOccurred, exception);
        }

        internal ICommand ShowDetailsPageCommand
        {
            get;
            private set;
        }

        private bool CanShowDetailsPage(object packageAction)
        {
            return
                this.SelectedPackage != null &&
                !this.IsLicensePageVisible &&
                !this.IsDetailsPaneVisible &&
                !this.IsUninstallPageVisible;
        }

        private void ShowDetailsPage(object packageAction)
        {
            this.PackageAction = (PackageViewModelAction)packageAction;

            this.Loading = true;
            this.LoadingMessage = Resources.String_PackageInformation;

            // We just need to access the packageViewModel.RemotePackage property. That forces it to 
            // initialize
            Task dependenciesTask = Task.Factory.StartNew(() => this.SelectedPackage.RemotePackage == null, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            _primaryTask = dependenciesTask.ContinueWith(EndShowDetailsPage, this.Scheduler);
        }

        private void EndShowDetailsPage(Task task)
        {
            try
            {
                Exception exception = task.Exception;
                if (task.IsCanceled)
                {
                    // If the task has been cancelled, simply return
                    return;
                }
                else if (task.IsFaulted)
                {
                    this.ShowError(task.Exception.Flatten().InnerException);
                }
                else
                {
                    this.IsDetailsPaneVisible = true;
                }
            }
            finally
            {
                this.Loading = false;
                LoadingMessage = null;
            }
        }

        internal ICommand ShowLicensePageCommand
        {
            get;
            private set;
        }

        private bool CanShowLicensePage(object ignore)
        {
            return
                this.SelectedPackage != null &&
                this.IsDetailsPaneVisible &&
                this.Message == null;
        }

        private void ShowLicensePage(object ignore)
        {
            Debug.Assert(SelectedPackage != null, "Must have a selected package.");

            if (SelectedPackage.HasDependencies)
            {
                Loading = true;
                LoadingMessage = Resources.String_PackageInformation;

                // We just need to access the packageViewModel.Dependencies property. That forces it to 
                // initialize
                Task dependenciesTask = Task.Factory.StartNew(() => SelectedPackage.Dependencies == null, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                _primaryTask = dependenciesTask.ContinueWith(EndShowLicensePage, this.Scheduler);
            }
            else
            {
                IsLicensePageVisible = true;
            }
        }

        private void EndShowLicensePage(Task task)
        {
            try
            {
                Exception exception = task.Exception;
                if (task.IsCanceled)
                {
                    // If the task has been cancelled, simply return
                    return;
                }
                else if (task.IsFaulted)
                {
                    this.ShowError(task.Exception.Flatten().InnerException);
                }
                else
                {
                    IsLicensePageVisible = true;
                }
            }
            finally
            {
                Loading = false;
                LoadingMessage = null;
            }
        }

        internal ICommand ShowLicensePageForAllCommand
        {
            get;
            private set;
        }

        private bool CanShowLicensePageForAll(object ignore)
        {
            return
                !this.IsLicensePageVisible &&
                !this.IsDetailsPaneVisible &&
                !this.IsUninstallPageVisible;
        }

        private void ShowLicensePageForAll(object ignore)
        {
            this.PackageAction = PackageViewModelAction.UpdateAll;
            PackagesToDisplayForUpdateAll.Clear();
            Loading = true;
            LoadingMessage = Resources.String_PackageInformation;

            Task<IEnumerable<IPackage>> dependenciesTask = Task.Factory.StartNew<IEnumerable<IPackage>>(() => _nuGetModel.PackageManager.GetPackagesToBeInstalledForUpdateAll(), 
                CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

            _primaryTask = dependenciesTask.ContinueWith(EndShowLicensePageForAll, this.Scheduler);
        }

        private void EndShowLicensePageForAll(Task<IEnumerable<IPackage>> task)
        {
            try
            {
                Exception exception = task.Exception;
                if (task.IsCanceled)
                {
                    // If the task has been cancelled, simply return
                    return;
                }
                else if (task.IsFaulted)
                {
                    this.ShowError(task.Exception.Flatten().InnerException);
                }
                else
                {
                    var packagesToDisplay = task.Result;
                    foreach (IPackage package in packagesToDisplay)
                    {
                        PackagesToDisplayForUpdateAll.Add(new PackageViewModel(_nuGetModel, package, PackageViewModelAction.Update));
                    }
                    IsLicensePageVisible = true;
                }
            }
            finally
            {
                Loading = false;
                LoadingMessage = null;
            }
        }

        internal ICommand ShowUninstallPageCommand
        {
            get;
            private set;
        }

        private bool CanShowUninstallPage(object ignore)
        {
            return
                this.SelectedPackage != null &&
                !this.SelectedPackage.IsMandatory &&
                !this.IsLicensePageVisible &&
                !this.IsDetailsPaneVisible &&
                !this.IsUninstallPageVisible;
        }

        private void ShowUninstallPage(object ignore)
        {
            this.IsUninstallPageVisible = true;
        }

        public ICommand ShowListCommand
        {
            get;
            private set;
        }

        private bool CanShowList(object ignore)
        {
            return
                this.IsLicensePageVisible ||
                this.IsDetailsPaneVisible ||
                this.IsUninstallPageVisible;
        }

        private void ShowList(object ignore)
        {
            this.IsLicensePageVisible = false;
            this.IsDetailsPaneVisible = false;
            this.IsUninstallPageVisible = false;
        }

        internal ICommand InstallCommand
        {
            get;
            private set;
        }

        private bool CanInstall(object ignore)
        {
            return
                this.SelectedPackage != null &&
                this.IsLicensePageVisible;
        }

        private void Install(object ignore)
        {
            Loading = true;
            LoadingMessage = Resources.String_Installing;
            IsLicensePageVisible = false;
            IsDetailsPaneVisible = false;

            var task = Task.Factory.StartNew(this.SelectedPackage.Install, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            _primaryTask = task.ContinueWith(EndInstall, this.Scheduler);
        }

        private void EndInstall(Task task)
        {
            Exception exception = task.Exception;
            if (task.IsCanceled)
            {
                // If the task has been cancelled, simply return
                return;
            }
            else if (task.IsFaulted)
            {
                this.ShowError(task.Exception.Flatten().InnerException);
            }

            this.BeginUpdateModel(this.SelectedFeedSource, _includePrerelease);
        }

        internal ICommand UpdateCommand
        {
            get;
            private set;
        }

        private bool CanUpdate(object ignore)
        {
            return
                this.SelectedPackage != null &&
                this.IsLicensePageVisible;
        }

        private void Update(object ignore)
        {
            Loading = true;
            LoadingMessage = Resources.String_Updating;
            IsLicensePageVisible = false;
            IsDetailsPaneVisible = false;

            var task = Task.Factory.StartNew(this.SelectedPackage.Update, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            _primaryTask = task.ContinueWith(EndUpdate, this.Scheduler);
        }

        private void EndUpdate(Task task)
        {
            Exception exception = task.Exception;
            if (task.IsCanceled)
            {
                // If the task has been cancelled, simply return
                return;
            }
            else if (task.IsFaulted)
            {
                this.ShowError(task.Exception.Flatten().InnerException);
            }

            this.BeginUpdateModel(this.SelectedFeedSource, _includePrerelease);
        }

        internal ICommand UpdateAllCommand
        {
            get;
            private set;
        }

        private bool CanUpdateAll(object ignore)
        {
            return this.IsLicensePageVisible;
        }

        private void UpdateAll(object ignore)
        {
            Loading = true;
            LoadingMessage = Resources.String_Updating;
            IsLicensePageVisible = false;
            IsDetailsPaneVisible = false;

            var task = Task.Factory.StartNew(UpdateAllPackages, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            _primaryTask = task.ContinueWith(EndUpdate, this.Scheduler);
        }

        private void UpdateAllPackages()
        {
            _nuGetModel.UpdateAllPackages(inDetails: true);
        }

        internal ICommand UninstallCommand
        {
            get;
            private set;
        }

        private bool CanUninstall(object ignore)
        {
            return
                this.SelectedPackage != null &&
                !this.SelectedPackage.IsMandatory &&
                this.IsUninstallPageVisible;
        }

        private void Uninstall(object ignore)
        {
            Loading = true;
            LoadingMessage = Resources.String_Uninstalling;
            IsUninstallPageVisible = false;
            IsDetailsPaneVisible = false;

            var task = Task.Factory.StartNew(this.SelectedPackage.Uninstall, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            _primaryTask = task.ContinueWith(EndUninstall, this.Scheduler);
        }

        private void EndUninstall(Task task)
        {
            Exception exception = task.Exception;
            if (task.IsCanceled)
            {
                // If the task has been cancelled, simply return
                return;
            }
            else if (task.IsFaulted)
            {
                this.ShowError(task.Exception.Flatten().InnerException);
            }

            this.BeginUpdateModel(this.SelectedFeedSource, _includePrerelease);
        }

        public ICommand DefaultActionCommand
        {
            get;
            private set;
        }

        private bool CanDefaultAction(object ignore)
        {
            return this.SelectedPackage != null;
        }

        private void DefaultAction(object ignore)
        {
            if (this.SelectedPackage == null)
            {
                return;
            }
            else if (this.SelectedPackage.IsInstalled)
            {
                if (this.SelectedPackage.IsMandatory)
                {
                    // No Default action for INSTALLED Mandatory Extensions
                    return;
                }

                this.ShowUninstallPage(null);
            }
            else
            {
                this.ShowDetailsPage(PackageViewModelAction.InstallOrUninstall);
            }
        }

        public ICommand DisableCommand
        {
            get;
            private set;
        }

        private bool CanDisable(object ignore)
        {
            return
                this.SelectedPackage != null &&
                this.SelectedPackage.SupportsEnableDisable &&
                this.SelectedPackage.IsEnabled;
        }

        private void Disable(object ignore)
        {
            this.ExecuteToggleEnableAction();
        }

        public ICommand EnableCommand
        {
            get;
            private set;
        }

        private bool CanEnable(object ignore)
        {
            return
                this.SelectedPackage != null &&
                this.SelectedPackage.SupportsEnableDisable &&
                !this.SelectedPackage.IsEnabled;
        }

        private void Enable(object ignore)
        {
            this.ExecuteToggleEnableAction();
        }

        private string LoadPrereleaseFilter()
        {
            if (_preferences != null && _preferences.ContainsValue(PrereleaseFilterKey))
            {
                return _preferences.GetValue(PrereleaseFilterKey);
            }

            // Default Value
            return Resources.Prerelease_Filter_StableOnly;
        }

        private void SavePrereleaseFilter(string prereleaseFilter)
        {
            if (_preferences != null)
            {
                _preferences.SetValue(PrereleaseFilterKey, prereleaseFilter);
            }
        }
    }
}

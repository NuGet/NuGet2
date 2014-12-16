using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using NuGet.Client.Installation;
using NuGet.Client.Resolution;
using NuGet.Versioning;
using NuGet.VisualStudio;
using NuGetConsole;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for PackageManagerControl.xaml
    /// </summary>
    public partial class PackageManagerControl : UserControl, IVsWindowSearch
    {
        private const int PageSize = 10;

        private bool _initialized;

        // used to prevent starting new search when we update the package sources
        // list in response to PackageSourcesChanged event.
        private bool _dontStartNewSearch;

        private int _busyCount;

        public PackageManagerModel Model { get; private set; }

        public SourceRepositoryManager Sources
        {
            get
            {
                return Model.Sources;
            }
        }

        public InstallationTarget Target
        {
            get
            {
                return Model.Target;
            }
        }

        private IConsole _outputConsole;

        internal IUserInterfaceService UI { get; private set; }

        private PackageRestoreBar _restoreBar;
        private IPackageRestoreManager _packageRestoreManager;

        private IVsWindowSearchHost _windowSearchHost;

        public PackageManagerControl(PackageManagerModel model, IUserInterfaceService ui)
        {
            UI = ui;
            Model = model;
            _busyCount = 0;

            InitializeComponent();

            var factory = ServiceLocator.GetGlobalService<SVsWindowSearchHostFactory, IVsWindowSearchHostFactory>();
            _windowSearchHost = factory.CreateWindowSearchHost(_searchControlParent);

            _filter.Items.Add(Resx.Resources.Filter_All);
            _filter.Items.Add(Resx.Resources.Filter_Installed);
            _filter.Items.Add(Resx.Resources.Filter_UpdateAvailable);

            // TODO: Relocate to v3 API.
            _packageRestoreManager = ServiceLocator.GetInstance<IPackageRestoreManager>();
            AddRestoreBar();

            _packageDetail.Control = this;

            var outputConsoleProvider = ServiceLocator.GetInstance<IOutputConsoleProvider>();
            _outputConsole = outputConsoleProvider.CreateOutputConsole(requirePowerShellHost: false);

            InitSourceRepoList();
            _initialized = true;

            Model.Sources.PackageSourcesChanged += Sources_PackageSourcesChanged;
        }

        private void Sources_PackageSourcesChanged(object sender, EventArgs e)
        {
            // Set _dontStartNewSearch to true to prevent a new search started in
            // _sourceRepoList_SelectionChanged(). This method will start the new
            // search when needed by itself.
            _dontStartNewSearch = true;
            try
            {
                var oldActiveSource = _sourceRepoList.SelectedItem as PackageSource;
                var newSources = new List<PackageSource>(Sources.AvailableSources);

                // Update the source repo list with the new value.
                _sourceRepoList.Items.Clear();

                foreach (var source in newSources)
                {
                    _sourceRepoList.Items.Add(source);
                }

                if (oldActiveSource != null && newSources.Contains(oldActiveSource))
                {
                    // active source is not changed. Set _dontStartNewSearch to true
                    // to prevent a new search when _sourceRepoList.SelectedItem is set.
                    _sourceRepoList.SelectedItem = oldActiveSource;
                }
                else
                {
                    // active source changed.
                    _sourceRepoList.SelectedItem =
                        newSources.Count > 0 ?
                        newSources[0] :
                        null;

                    // start search explicitly.
                    SearchPackageInActivePackageSource();
                }
            }
            finally
            {
                _dontStartNewSearch = false;
            }
        }

        private void AddRestoreBar()
        {
            _restoreBar = new PackageRestoreBar(_packageRestoreManager);
            _root.Children.Add(_restoreBar);
            _packageRestoreManager.PackagesMissingStatusChanged += packageRestoreManager_PackagesMissingStatusChanged;
        }

        private void RemoveRestoreBar()
        {
            if (_restoreBar != null)
            {
                _restoreBar.CleanUp();
                _packageRestoreManager.PackagesMissingStatusChanged -= packageRestoreManager_PackagesMissingStatusChanged;
            }
        }

        private void packageRestoreManager_PackagesMissingStatusChanged(object sender, PackagesMissingStatusEventArgs e)
        {
            // PackageRestoreManager fires this event even when solution is closed.
            // Don't do anything if solution is closed.
            if (!Target.IsAvailable)
            {
                return;
            }

            if (!e.PackagesMissing)
            {
                // packages are restored. Update the UI
                if (Target.IsSolution)
                {
                    // TODO: update UI here
                }
                else
                {
                    // TODO: update UI here
                }
            }
        }

        private void InitSourceRepoList()
        {
            _label.Text = string.Format(
                CultureInfo.CurrentCulture,
                Resx.Resources.Label_PackageManager,
                Target.Name);

            // init source repo list
            _sourceRepoList.Items.Clear();
            foreach (var source in Sources.AvailableSources)
            {
                _sourceRepoList.Items.Add(source);
            }

            if (Sources.ActiveRepository != null)
            {
                _sourceRepoList.SelectedItem = Sources.ActiveRepository.Source;
            }
        }

        private void SetBusy(bool busy)
        {
            if (busy)
            {
                _busyCount++;
                if (_busyCount > 0)
                {
                    _busyControl.Visibility = System.Windows.Visibility.Visible;
                    this.IsEnabled = false;
                }
            }
            else
            {
                _busyCount--;
                if (_busyCount <= 0)
                {
                    _busyControl.Visibility = System.Windows.Visibility.Collapsed;
                    this.IsEnabled = true;
                }
            }
        }

        private bool ShowInstalled
        {
            get
            {
                return Resx.Resources.Filter_Installed.Equals(_filter.SelectedItem);
            }
        }

        private bool ShowUpdatesAvailable
        {
            get
            {
                return Resx.Resources.Filter_UpdateAvailable.Equals(_filter.SelectedItem);
            }
        }

        public bool IncludePrerelease
        {
            get
            {
                return _checkboxPrerelease.IsChecked == true;
            }
        }

        internal SourceRepository CreateActiveRepository()
        {
            var activeSource = _sourceRepoList.SelectedItem as PackageSource;
            if (activeSource == null)
            {
                return null;
            }

            return Sources.CreateSourceRepository(activeSource);
        }

        private void SearchPackageInActivePackageSource()
        {
            var searchText = _windowSearchHost.SearchQuery.SearchString;            
            var activeSource = _sourceRepoList.SelectedItem as PackageSource;
            var sourceRepository = Sources.CreateSourceRepository(activeSource);

            Filter filter = Filter.All;
            if (Resx.Resources.Filter_Installed.Equals(_filter.SelectedItem))
            {
                filter = Filter.Installed;
            }
            else if (Resx.Resources.Filter_UpdateAvailable.Equals(_filter.SelectedItem))
            {
                filter = Filter.UpdatesAvailable;
            }
            PackageLoaderOption option = new PackageLoaderOption(
                filter,
                IncludePrerelease);

            var loader = new PackageLoader(
                sourceRepository,
                Target,
                option,
                searchText);
            _packageList.Loader = loader;
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            UI.LaunchNuGetOptionsDialog();
        }

        private void PackageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDetailPane();
        }

        /// <summary>
        /// Updates the detail pane based on the selected package
        /// </summary>
        private async void UpdateDetailPane()
        {
            var selectedPackage = _packageList.SelectedItem as UiSearchResultPackage;
            if (selectedPackage == null)
            {
                _packageDetail.DataContext = null;
            }
            else
            {
                DetailControlModel newModel;
                if (Target.IsSolution)
                {
                    newModel = new PackageSolutionDetailControlModel(
                        (VsSolution)Target,
                        selectedPackage);
                }
                else
                {
                    newModel = new PackageDetailControlModel(
                        Target,
                        selectedPackage);
                }

                var oldModel = _packageDetail.DataContext as DetailControlModel;
                if (oldModel != null)
                {
                    newModel.Options = oldModel.Options;
                }
                _packageDetail.DataContext = newModel;
                await newModel.LoadPackageMetadaAsync();
            }
        }

        private void _sourceRepoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dontStartNewSearch)
            {
                return;
            }

            var newSource = _sourceRepoList.SelectedItem as PackageSource;
            if (newSource != null)
            {
                Sources.ChangeActiveSource(newSource);
            }
            SearchPackageInActivePackageSource();
        }

        private void _filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initialized)
            {
                SearchPackageInActivePackageSource();
            }
        }

        internal void UpdatePackageStatus()
        {
            if (ShowInstalled || ShowUpdatesAvailable)
            {
                // refresh the whole package list
                _packageList.Reload();
            }
            else
            {
                // in this case, we only need to update PackageStatus of
                // existing items in the package list
                foreach (var item in _packageList.Items)
                {
                    var package = item as UiSearchResultPackage;
                    if (package == null)
                    {
                        continue;
                    }

                    package.Status = PackageManagerControl.GetPackageStatus(
                        package.Id,
                        Target,
                        package.Versions);
                }
            }
        }

        /// <summary>
        /// Gets the status of the package specified by <paramref name="packageId"/> in
        /// the specified installation target.
        /// </summary>
        /// <param name="packageId">package id.</param>
        /// <param name="target">The installation target.</param>
        /// <param name="allVersions">List of all versions of the package.</param>
        /// <returns>The status of the package in the installation target.</returns>
        public static PackageStatus GetPackageStatus(
            string packageId,
            InstallationTarget target,
            IEnumerable<NuGetVersion> allVersions)
        {
            var latestStableVersion = allVersions
                .Where(p => !p.IsPrerelease)
                .Max(p => p);

            // Get the minimum version installed in any target project/solution
            var minimumInstalledPackage = target.GetAllTargetsRecursively()
                .Select(t => t.InstalledPackages.GetInstalledPackage(packageId))
                .Where(p => p != null)
                .OrderBy(r => r.Identity.Version)
                .FirstOrDefault();

            PackageStatus status;
            if (minimumInstalledPackage != null)
            {
                if (minimumInstalledPackage.Identity.Version < latestStableVersion)
                {
                    status = PackageStatus.UpdateAvailable;
                }
                else
                {
                    status = PackageStatus.Installed;
                }
            }
            else
            {
                status = PackageStatus.NotInstalled;
            }

            return status;
        }

        public bool ShowLicenseAgreement(IEnumerable<PackageAction> operations)
        {
            var licensePackages = operations.Where(op =>
                op.ActionType == PackageActionType.Install &&
                op.Package.Value<bool>("requireLicenseAcceptance"));

            // display license window if necessary
            if (licensePackages.Any())
            {
                // Hacky distinct without writing a custom comparer
                var licenseModels = licensePackages
                    .GroupBy(a => Tuple.Create(a.Package["id"], a.Package["version"]))
                    .Select(g =>
                    {
                        dynamic p = g.First().Package;
                        string licenseUrl = (string)p.licenseUrl;
                        string id = (string)p.id;
                        string authors = (string)p.authors;

                        return new PackageLicenseInfo(
                            id,
                            licenseUrl == null ? null : new Uri(licenseUrl),
                            authors);
                    })
                    .Where(pli => pli.LicenseUrl != null); // Shouldn't get nulls, but just in case

                bool accepted = this.UI.PromptForLicenseAcceptance(licenseModels);
                if (!accepted)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Shows the preveiw window for the actions.
        /// </summary>
        /// <param name="actions">actions to preview.</param>
        /// <returns>True if nuget should continue to perform the actions. Otherwise false.</returns>
        private bool PreviewActions(IEnumerable<PackageAction> actions)
        {
            var w = new PreviewWindow();
            w.DataContext = new PreviewWindowModel(actions, Target);
            return w.ShowModal() == true;
        }

        // perform the user selected action
        internal async void PerformAction(DetailControl detailControl)
        {
            SetBusy(true);
            _outputConsole.Clear();
            var progressDialog = new ProgressDialog(_outputConsole);
            progressDialog.Owner = Window.GetWindow(this);
            progressDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            try
            {
                var actions = await detailControl.ResolveActionsAsync();

                // show preview
                var model = (DetailControlModel)_packageDetail.DataContext;
                if (model.Options.ShowPreviewWindow)
                {
                    var shouldContinue = PreviewActions(actions);
                    if (!shouldContinue)
                    {
                        return;
                    }
                }

                // show license agreeement
                bool acceptLicense = ShowLicenseAgreement(actions);
                if (!acceptLicense)
                {
                    return;
                }

                // Create the executor and execute the actions
                progressDialog.FileConflictAction = detailControl.FileConflictAction;
                progressDialog.Show();
                var executor = new ActionExecutor();
                await Task.Run(
                    () =>
                    {
                        executor.ExecuteActions(actions, progressDialog);
                    });

                UpdatePackageStatus();
                detailControl.Refresh();
            }
            catch (Exception ex)
            {
                var errorDialog = new ErrorReportingDialog(
                    ex.Message,
                    ex.ToString());
                errorDialog.ShowModal();
            }
            finally
            {
                progressDialog.RequestToClose();
                SetBusy(false);
            }
        }

        private void _searchControl_SearchStart(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                return;
            }

            SearchPackageInActivePackageSource();
        }

        private void _checkboxPrerelease_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_initialized)
            {
                return;
            }

            SearchPackageInActivePackageSource();
        }

        private void ControlLoaded(object sender, RoutedEventArgs e)
        {
            _windowSearchHost.SetupSearch(this);
            _windowSearchHost.IsVisible = true;
        }

        private void ControlUnloaded(object sender, RoutedEventArgs e)
        {
            _windowSearchHost.TerminateSearch();
            RemoveRestoreBar();
        }

        public Guid Category
        {
            get
            {
                return Guid.Empty;
            }
        }

        public void ClearSearch()
        {
            SearchPackageInActivePackageSource();
        }

        public IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        {
            SearchPackageInActivePackageSource();
            return null;
        }

        public bool OnNavigationKeyDown(uint dwNavigationKey, uint dwModifiers)
        {
            // We are not interesting in intercepting navigation keys, so return "not handled"
            return false;
        }

        public void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
        {
            var settings = (SearchSettingsDataSource)pSearchSettings;
            settings.ControlMinWidth = (uint)_searchControlParent.MinWidth;
            settings.ControlMaxWidth = uint.MaxValue;
        }

        public bool SearchEnabled
        {
            get { return true; }
        }

        public IVsEnumWindowSearchFilters SearchFiltersEnum
        {
            get { return null; }
        }

        public IVsEnumWindowSearchOptions SearchOptionsEnum
        {
            get { return null; }
        }
    }
}
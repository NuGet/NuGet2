using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    public partial class PackageManagerControl : UserControl
    {
        private const int PageSize = 10;

        // Copied from file Constants.cs in NuGet.Core:
        // This is temporary until we fix the gallery to have proper first class support for this.
        // The magic unpublished date is 1900-01-01T00:00:00
        public static readonly DateTimeOffset Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));

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

        public PackageManagerControl(PackageManagerModel model, IUserInterfaceService ui)
        {
            UI = ui;
            Model = model;
            _busyCount = 0;

            InitializeComponent();

            _searchControl.Text = model.SearchText;
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
            this.Unloaded += PackageManagerControl_Unloaded;
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

        private void PackageManagerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            RemoveRestoreBar();
        }

        private void AddRestoreBar()
        {
            _restoreBar = new PackageRestoreBar(_packageRestoreManager);
            _root.Children.Add(_restoreBar);
            _packageRestoreManager.PackagesMissingStatusChanged += packageRestoreManager_PackagesMissingStatusChanged;
        }

        private void RemoveRestoreBar()
        {
            _restoreBar.CleanUp();
            _packageRestoreManager.PackagesMissingStatusChanged -= packageRestoreManager_PackagesMissingStatusChanged;
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

        private class PackageLoaderOption
        {
            public bool IncludePrerelease { get; set; }

            public bool ShowUpdatesAvailable { get; set; }
        }

        private class PackageLoader : ILoader
        {
            // where to get the package list
            private Func<int, CancellationToken, Task<IEnumerable<JObject>>> _loader;

            private InstallationTarget _target;

            private PackageLoaderOption _option;

            private SourceRepository _source;

            public PackageLoader(
                Func<int, CancellationToken, Task<IEnumerable<JObject>>> loader,
                SourceRepository source,
                InstallationTarget target,
                PackageLoaderOption option,
                string searchText)
            {
                _loader = loader;
                _target = target;
                _option = option;
                _source = source;

                LoadingMessage = string.IsNullOrWhiteSpace(searchText) ?
                    Resx.Resources.Text_Loading :
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resx.Resources.Text_Searching,
                        searchText);
            }

            public string LoadingMessage
            {
                get;
                private set;
            }

            public async Task<LoadResult> LoadItems(int startIndex, CancellationToken ct)
            {
                var results = await _loader(startIndex, ct);

                List<UiSearchResultPackage> packages = new List<UiSearchResultPackage>();
                int resultCount = 0;
                foreach (var package in results)
                {
                    ct.ThrowIfCancellationRequested();
                    ++resultCount;

                    var searchResultPackage = new UiSearchResultPackage(_source);
                    searchResultPackage.Id = package.Value<string>(Properties.PackageId);
                    searchResultPackage.Version = NuGetVersion.Parse(package.Value<string>(Properties.LatestVersion));

                    searchResultPackage.IconUrl = GetUri(package, Properties.IconUrl);

                    // get other versions
                    var versionList = new List<NuGetVersion>();
                    var versions = package.Value<JArray>(Properties.Versions);
                    if (versions != null)
                    {   
                        if (versions[0].Type == JTokenType.String)
                        {
                            // TODO: this part should be removed once the new end point is up and running.
                            versionList = versions
                                .Select(v => NuGetVersion.Parse(v.Value<string>()))
                                .ToList();
                        }
                        else
                        {
                            versionList = versions
                                .Select(v => NuGetVersion.Parse(v.Value<string>("version")))
                                .ToList();
                        }

                        if (!_option.IncludePrerelease)
                        {
                            // remove prerelease version if includePrelease is false
                            versionList.RemoveAll(v => v.IsPrerelease);
                        }
                    }
                    if (!versionList.Contains(searchResultPackage.Version))
                    {
                        versionList.Add(searchResultPackage.Version);
                    }

                    searchResultPackage.Versions = versionList;
                    searchResultPackage.Status = PackageManagerControl.GetPackageStatus(
                        searchResultPackage.Id,
                        _target,
                        searchResultPackage.Versions);

                    // filter out prerelease version when needed.
                    if (searchResultPackage.Version.IsPrerelease &&
                       !_option.IncludePrerelease &&
                        searchResultPackage.Status == PackageStatus.NotInstalled)
                    {
                        continue;
                    }

                    if (_option.ShowUpdatesAvailable &&
                        searchResultPackage.Status != PackageStatus.UpdateAvailable)
                    {
                        continue;
                    }

                    searchResultPackage.Summary = package.Value<string>(Properties.Summary);
                    if (string.IsNullOrWhiteSpace(searchResultPackage.Summary))
                    {
                        // summary is empty. Use its description instead.
                        searchResultPackage.Summary = package.Value<string>(Properties.Description);
                    }

                    packages.Add(searchResultPackage);
                }

                ct.ThrowIfCancellationRequested();
                return new LoadResult()
                {
                    Items = packages,
                    HasMoreItems = resultCount == PageSize,
                    NextStartIndex = startIndex + resultCount
                };
            }

            // Get all versions of the package
            private List<UiPackageMetadata> LoadVersions(JArray versions, NuGetVersion searchResultVersion)
            {
                var retValue = new List<UiPackageMetadata>();
                if (versions == null)
                {
                    return retValue;
                }

                // If repo is AggregateRepository, the package duplicates can be returned by
                // FindPackagesById(), so Distinct is needed here to remove the duplicates.
                foreach (var token in versions)
                {
                    Debug.Assert(token.Type == JTokenType.Object);
                    JObject version = (JObject)token;
                    var detailedPackage = DetailControlModel.CreateDetailedPackage(version);

                    if (detailedPackage.Version.IsPrerelease &&
                        !_option.IncludePrerelease &&
                        detailedPackage.Version != searchResultVersion)
                    {
                        // don't include prerelease version if includePrerelease is false
                        continue;
                    }

                    if (detailedPackage.Published <= Unpublished &&
                        detailedPackage.Version != searchResultVersion)
                    {
                        // don't include unlisted package
                        continue;
                    }

                    retValue.Add(detailedPackage);
                }

                return retValue;
            }

            private Uri GetUri(JObject json, string property)
            {
                if (json[property] == null)
                {
                    return null;
                }
                string str = json[property].ToString();
                if (String.IsNullOrEmpty(str))
                {
                    return null;
                }
                return new Uri(str);
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
            var searchText = _searchControl.Text;
            var supportedFrameworks = Target.GetSupportedFrameworks();

            // search online
            var activeSource = _sourceRepoList.SelectedItem as PackageSource;
            var sourceRepository = Sources.CreateSourceRepository(activeSource);

            PackageLoaderOption option = new PackageLoaderOption()
            {
                IncludePrerelease = this.IncludePrerelease,
                ShowUpdatesAvailable = this.ShowUpdatesAvailable
            };

            if (ShowInstalled || ShowUpdatesAvailable)
            {
                // search installed packages
                var loader = new PackageLoader(
                    (startIndex, ct) =>
                        Target.SearchInstalled(
                            sourceRepository,
                            searchText,
                            startIndex,
                            PageSize,
                            ct),
                    sourceRepository,
                    Target,
                    option,
                    searchText);
                _packageList.Loader = loader;
            }
            else
            {
                // search in active package source
                if (activeSource == null)
                {
                    var loader = new PackageLoader(
                        (startIndex, ct) =>
                        {
                            return Task.Factory.StartNew(() =>
                            {
                                return Enumerable.Empty<JObject>();
                            });
                        },
                        sourceRepository,
                        Target,
                        option,
                        searchText);
                    _packageList.Loader = loader;
                }
                else
                {
                    var loader = new PackageLoader(
                        (startIndex, ct) =>
                            sourceRepository.Search(
                            searchText,
                            new SearchFilter()
                            {
                                SupportedFrameworks = supportedFrameworks,
                                IncludePrerelease = option.IncludePrerelease
                            },
                            startIndex,
                            PageSize,
                            ct),
                        sourceRepository,
                        Target,
                        option,
                        searchText);
                    _packageList.Loader = loader;
                }
            }
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
        private static PackageStatus GetPackageStatus(
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
    }
}
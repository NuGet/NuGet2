using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using NuGet.VisualStudio;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for PackageManagerControl.xaml
    /// </summary>
    public partial class PackageManagerControl : UserControl
    {
        private const int PageSize = 15;

        private bool _initialized;

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

        internal IUserInterfaceService UI { get; private set; }

        // Indicates if the user is managing packages for a project or for the solution
        private bool _forProject;

        // applicable when _forProject if false
        VsSolutionInstallationTarget _solutionTarget;

        private PackageRestoreBar _restoreBar;
        private IPackageRestoreManager _packageRestoreManager;

        public PackageManagerControl(PackageManagerModel myDoc, IUserInterfaceService ui)
        {
            UI = ui;
            Model = myDoc;

            InitializeComponent();

            _packageRestoreManager = ServiceLocator.GetInstance<IPackageRestoreManager>();
            AddRestoreBar();

            _packageDetail.Visibility = System.Windows.Visibility.Collapsed;
            _packageDetail.Control = this;

            _packageSolutionDetail.Visibility = System.Windows.Visibility.Collapsed;
            _packageSolutionDetail.Control = this;

            if (Target is VsProjectInstallationTarget)
            {
                _forProject = true;
                _packageDetail.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                _forProject = false;
                _solutionTarget = (VsSolutionInstallationTarget)Target;
                _packageSolutionDetail.Visibility = System.Windows.Visibility.Visible;
            }

            Update();
            this.Unloaded += PackageManagerControl_Unloaded;
            _initialized = true;
        }

        void PackageManagerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            RemoveRestoreBar();
        }

        void AddRestoreBar()
        {
            _restoreBar = new PackageRestoreBar(_packageRestoreManager);
            _root.Children.Add(_restoreBar);
            _packageRestoreManager.PackagesMissingStatusChanged += packageRestoreManager_PackagesMissingStatusChanged;
        }

        void RemoveRestoreBar()
        {
            _restoreBar.CleanUp();
            _packageRestoreManager.PackagesMissingStatusChanged -= packageRestoreManager_PackagesMissingStatusChanged;
        }

        void packageRestoreManager_PackagesMissingStatusChanged(object sender, PackagesMissingStatusEventArgs e)
        {
            // PackageRestoreManager fires this event even when solution is closed. 
            // Don't do anything if solution is closed.
            if (!Target.IsSolutionOpen)
            {
                return;
            }

            if (!e.PackagesMissing)
            {
                // packages are restored. Update the UI
                if (_forProject)
                {
                }
                else
                {
                    _solutionTarget.CreateInstalledPackages();
                    UpdateDetailPane();
                }
            }
        }

        private void Update()
        {
            _label.Content = string.Format(
                CultureInfo.CurrentCulture,
                Resx.Resources.Label_PackageManager,
                Target.Name);

            // init source repo list
            _sourceRepoList.Items.Clear();
            foreach (var source in Sources.AvailableSources)
            {
                _sourceRepoList.Items.Add(source);
            }
            _sourceRepoList.SelectedItem = Sources.ActiveRepository.Source;

            SearchPackageInActivePackageSource();
        }

        public void SetBusy(bool busy)
        {
            if (busy)
            {
                _busyControl.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                _busyControl.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void SetPackageListBusy(bool busy)
        {
            if (busy)
            {
                _listBusyControl.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                _listBusyControl.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private class PackageLoader : ILoader
        {
            // where to get the package list
            private Func<int, CancellationToken, Task<IEnumerable<JObject>>> _loader;

            private InstallationTarget _target;

            public PackageLoader(
                Func<int, CancellationToken, Task<IEnumerable<JObject>>> loader,
                InstallationTarget target)
            {
                _loader = loader;
                _target = target;
            }

            public async Task<LoadResult> LoadItems(int startIndex, CancellationToken ct)
            {
                var results = await _loader(startIndex, ct);

                List<UiSearchResultPackage> packages = new List<UiSearchResultPackage>();
                foreach (var package in results)
                {
                    ct.ThrowIfCancellationRequested();

                    // As a debugging aide, I am intentionally NOT using an object initializer -anurse
                    var searchResultPackage = new UiSearchResultPackage();
                    searchResultPackage.Id = package.Value<string>("id");
                    searchResultPackage.Version = NuGetVersion.Parse(package.Value<string>("latestVersion"));
                    searchResultPackage.Summary = package.Value<string>("summary");
                    searchResultPackage.IconUrl = package.Value<Uri>("iconUrl");

                    var installedPackage = _target.Installed.GetInstalledPackage(searchResultPackage.Id);
                    if (installedPackage != null)
                    {
                        if (installedPackage.Identity.Version < searchResultPackage.Version)
                        {
                            searchResultPackage.Status = PackageStatus.UpdateAvailable;
                        }
                        else
                        {
                            searchResultPackage.Status = PackageStatus.Installed;
                        }
                    }
                    else
                    {
                        searchResultPackage.Status = PackageStatus.NotInstalled;
                    }

                    searchResultPackage.AllVersions = LoadVersions(package.Value<JArray>("packages"));
                    packages.Add(searchResultPackage);
                }

                ct.ThrowIfCancellationRequested();
                return new LoadResult()
                {
                    Items = packages,
                    HasMoreItems = packages.Count == PageSize
                };
            }

            // Get all versions of the package
            private List<UiDetailedPackage> LoadVersions(JArray versions)
            {
                var retValue = new List<UiDetailedPackage>();

                // If repo is AggregateRepository, the package duplicates can be returned by
                // FindPackagesById(), so Distinct is needed here to remove the duplicates.
                foreach (var token in versions)
                {
                    JObject version = (JObject)token;
                    var detailedPackage = new UiDetailedPackage()
                    {
                        Id = version.Value<string>("id"),
                        Version = NuGetVersion.Parse(version.Value<string>("version")),
                        Summary = version.Value<string>("summary"),
                        Description = version.Value<string>("description"),
                        Authors = StringCollectionToString(version.Value<JArray>("authors")),
                        Owners = StringCollectionToString(version.Value<JArray>("owners")),
                        IconUrl = version.Value<Uri>("iconUrl"),
                        LicenseUrl = version.Value<Uri>("licenseUrl"),
                        ProjectUrl = version.Value<Uri>("projectUrl"),
                        Tags = String.Join(" ", (version.Value<JArray>("tags") ?? Enumerable.Empty<JToken>()).Select(t => t.ToString())),
                        DownloadCount = version.Value<int>("downloadCount"),
                        DependencySets = (version.Value<JArray>("dependencyGroups") ?? Enumerable.Empty<JToken>()).Select(obj => LoadDependencySet((JObject)obj))
                    };

                    string publishedStr = version.Value<string>("published");
                    if (!String.IsNullOrEmpty(publishedStr))
                    {
                        detailedPackage.Published = DateTime.Parse(publishedStr);
                    }
                    detailedPackage.HasDependencies = detailedPackage.DependencySets.Any(
                        set => set.Dependencies != null && set.Dependencies.Count > 0);

                    retValue.Add(detailedPackage);
                }

                return retValue;
            }

            private UiPackageDependencySet LoadDependencySet(JObject set)
            {
                var fxName = set.Value<string>("targetFramework");
                return new UiPackageDependencySet(
                    String.IsNullOrEmpty(fxName) ? null : new FrameworkName(fxName),
                    (set.Value<JArray>("dependencies") ?? Enumerable.Empty<JToken>()).Select(obj => LoadDependency((JObject)obj)));
            }

            private UiPackageDependency LoadDependency(JObject dep)
            {
                var ver = dep.Value<string>("range");
                return new UiPackageDependency(
                    dep.Value<string>("id"),
                    String.IsNullOrEmpty(ver) ? null : VersionRange.Parse(ver));
            }

            private string StringCollectionToString(JArray v)
            {
                if (v == null)
                {
                    return null;
                }

                string retValue = String.Join(", ", v.Select(t => t.ToString()));
                if (retValue == String.Empty)
                {
                    return null;
                }

                return retValue;
            }
        }

        private void SearchPackageInActivePackageSource()
        {
            var searchText = _searchText.Text;
            bool showOnlyInstalled = _filter.SelectedIndex == 1;
            var supportedFrameworks = Target.GetSupportedFrameworks();

            if (showOnlyInstalled)
            {
                var loader = new PackageLoader(
                    (startIndex, ct) =>
                        Target.Installed.Search(
                            searchText,
                            startIndex,
                            PageSize,
                            ct),
                    Target);
                _packageList.Loader = loader;
            }
            else
            {
                // search online
                var loader = new PackageLoader(
                    (startIndex, ct) =>
                        Sources.ActiveRepository.Search(
                            searchText,
                            new SearchFilter()
                            {
                                SupportedFrameworks = supportedFrameworks,
                                IncludePrerelease = false
                            },
                            startIndex,
                            PageSize,
                            ct),
                    Target);
                _packageList.Loader = loader;
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
        private void UpdateDetailPane()
        {
            var selectedPackage = _packageList.SelectedItem as UiSearchResultPackage;
            if (selectedPackage == null)
            {
                if (_forProject)
                {
                    _packageDetail.DataContext = null;
                }
                else
                {
                    _packageSolutionDetail.DataContext = null;
                }
            }
            else
            {
                if (_forProject)
                {
                    var installedPackage = Target.Installed.GetInstalledPackage(selectedPackage.Id);
                    var installedVersion = installedPackage == null ? null : installedPackage.Identity.Version;
                    _packageDetail.DataContext = new PackageDetailControlModel(selectedPackage, installedVersion);
                }
                else
                {
                    _packageSolutionDetail.DataContext = new PackageSolutionDetailControlModel(
                        selectedPackage,
                        (SolutionInstalledPackageList)_solutionTarget.Installed);
                }
            }
        }

        private void _searchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SearchPackageInActivePackageSource();
            }
        }

        private void _sourceRepoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var newSource = _sourceRepoList.SelectedItem as PackageSource;
            if (newSource == null)
            {
                return;
            }

            Sources.ChangeActiveSource(newSource);
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
            var installedPackages = new Dictionary<string, SemanticVersion>(StringComparer.OrdinalIgnoreCase);

            // IInstalledPackageList makes for a slightly weird call here... may want to revisit some method naming :)
            foreach (var packageReference in Target.Installed.GetInstalledPackageReferences())
            {
                installedPackages[packageReference.Identity.Id] = packageReference.Identity.Version;
            }

            foreach (var item in _packageList.ItemsSource)
            {
                var package = item as UiSearchResultPackage;
                if (package == null)
                {
                    continue;
                }

                SemanticVersion installedVersion;
                if (installedPackages.TryGetValue(package.Id, out installedVersion))
                {
                    if (installedVersion < package.Version)
                    {
                        package.Status = PackageStatus.UpdateAvailable;
                    }
                    else
                    {
                        package.Status = PackageStatus.Installed;
                    }
                }
                else
                {
                    package.Status = PackageStatus.NotInstalled;
                }
            }
        }
    }
}
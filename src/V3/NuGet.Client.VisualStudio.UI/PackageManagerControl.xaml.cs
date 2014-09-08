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
using NuGet.Client;
using NuGet.Versioning;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for PackageManagerControl.xaml
    /// </summary>
    public partial class PackageManagerControl : UserControl
    {
        private bool _initialized;

        public PackageManagerModel Model { get; private set; }

        public PackageManagerSession Session
        {
            get
            {
                return Model.Session;
            }
        }

        internal IUserInterfaceService UI { get; private set; }

        public PackageManagerControl(PackageManagerModel myDoc, IUserInterfaceService ui)
        {
            UI = ui;
            Model = myDoc;

            InitializeComponent();

            _packageDetail.Control = this;
            Update();
            _initialized = true;
        }

        private void Update()
        {
            _label.Content = string.Format(CultureInfo.CurrentCulture,
                "Package Manager: {0}",
                Session.Name);

            // init source repo list
            _sourceRepoList.Items.Clear();
            var sources = Session.GetAvailableSources();
            foreach (var source in sources)
            {
                _sourceRepoList.Items.Add(source);
            }
            _sourceRepoList.SelectedItem = Session.ActiveSource;

            UpdatePackageList();
        }

        private void UpdatePackageList()
        {
            //if (_model.Project != null)
            //{
                SearchPackageInActivePackageSource();
            //}
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
            private string _searchText;
            private IEnumerable<FrameworkName> _supportedFrameworks;

            // where to get the package list
            private IPackageSearcher _searcher;
            private IInstalledPackageList _installed;

            private const int pageSize = 15;

            public PackageLoader(
                IPackageSearcher searcher,
                IInstalledPackageList installed,
                string searchText,
                IEnumerable<FrameworkName> supportedFrameworks)
            {
                _searcher = searcher;
                _installed = installed;

                _searchText = searchText;
                _supportedFrameworks = supportedFrameworks;
            }

            public async Task<LoadResult> LoadItems(int startIndex, CancellationToken ct)
            {
                var filter = new SearchFilter()
                {
                    SupportedFrameworks = _supportedFrameworks,
                    IncludePrerelease = false
                };

                var query = await _searcher.Search(_searchText, filter, startIndex, pageSize, ct);

                List<UiSearchResultPackage> packages = new List<UiSearchResultPackage>();
                foreach (JObject p in query)
                {
                    ct.ThrowIfCancellationRequested();

                    // As a debugging aide, I am intentionally NOT using an object initializer -anurse
                    var searchResultPackage = new UiSearchResultPackage();
                    searchResultPackage.Id = p.Value<string>("id");
                    searchResultPackage.Version = NuGetVersion.Parse(p.Value<string>("latestVersion"));
                    searchResultPackage.Summary = p.Value<string>("summary");
                    searchResultPackage.IconUrl = p.Value<Uri>("iconUrl");

                    var installedVersion = _installed.GetInstalledVersion(searchResultPackage.Id);
                    if (installedVersion != null)
                    {
                        if (installedVersion < searchResultPackage.Version)
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

                    searchResultPackage.AllVersions = LoadVersions(p.Value<JArray>("packages"));
                    packages.Add(searchResultPackage);
                }

                ct.ThrowIfCancellationRequested();
                return new LoadResult()
                {
                    Items = packages,
                    HasMoreItems = packages.Count == pageSize
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
                    String.IsNullOrEmpty(fxName) ? null : FrameworkNameHelpers.ParseFrameworkName(fxName),
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
            var supportedFrameworks = Session.GetSupportedFrameworks();
            
            if (showOnlyInstalled)
            {
                var loader = new PackageLoader(
                    Session.GetInstalledPackageList().CreateSearcher(),
                    Session.GetInstalledPackageList(),
                    searchText,
                    supportedFrameworks);
                _packageList.Loader = loader;
            }
            else
            {
                // search online                
                var loader = new PackageLoader(
                    Session.CreateSearcher(),
                    Session.GetInstalledPackageList(),
                    searchText,
                    supportedFrameworks);
                _packageList.Loader = loader;
            }
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            UI.LaunchNuGetOptionsDialog();
        }

        private void PackageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPackage = _packageList.SelectedItem as UiSearchResultPackage;
            if (selectedPackage == null)
            {
                _packageDetail.DataContext = null;
            }
            else
            {
                var installedVersion = Session.GetInstalledPackageList().GetInstalledVersion(selectedPackage.Id);
                _packageDetail.DataContext = new PackageDetailControlModel(selectedPackage, installedVersion);
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

            Session.ChangeActiveSource(newSource);
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
            foreach (var package in Session.GetInstalledPackageList().GetInstalledPackages())
            {
                installedPackages[package.Id] = package.Version;
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
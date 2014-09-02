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
using NuGet.VisualStudio;

namespace NuGet.Tools
{
    /// <summary>
    /// Interaction logic for PackageManagerControl.xaml
    /// </summary>
    public partial class PackageManagerControl : UserControl
    {
        private bool _initialized;

        public PackageManagerDocData Model { get; private set; }
        public PackageManagerSession Session
        {
            get
            {
                return Model.Session;
            }
        }

        public PackageManagerControl(PackageManagerDocData myDoc)
        {
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

                    var searchResultPackage = new UiSearchResultPackage()
                    {
                        // TODO: Use JSON-LD aware objects
                        Id = p.GetScalar<string>(Uris.Properties.PackageId),
                        Version = SemanticVersion.Parse(p.GetScalar<string>(Uris.Properties.LatestVersion)),
                        Summary = p.GetScalar<string>(Uris.Properties.Summary),
                        IconUrl = p.GetScalarUri(Uris.Properties.IconUrl)
                    };

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

                    searchResultPackage.AllVersions = LoadVersions((JArray)p[Uris.Properties.PackageVersion.AbsoluteUri]);
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
                foreach (var version in versions.Cast<JObject>())
                {
                    var detailedPackage = new UiDetailedPackage()
                    {
                        Id = version.GetScalar<string>(Uris.Properties.PackageId),
                        Version = SemanticVersion.Parse(version.GetScalar<string>(Uris.Properties.Version)),
                        Summary = version.GetScalar<string>(Uris.Properties.Summary),
                        Description = version.GetScalar<string>(Uris.Properties.Description),
                        Authors = StringCollectionToString(version.GetArray<string>(Uris.Properties.Author)),
                        Owners = StringCollectionToString(version.GetArray<string>(Uris.Properties.Owner)),
                        IconUrl = version.GetScalarUri(Uris.Properties.IconUrl),
                        LicenseUrl = version.GetScalarUri(Uris.Properties.LicenseUrl),
                        ProjectUrl = version.GetScalarUri(Uris.Properties.ProjectUrl),
                        Tags = version.GetScalar<string>(Uris.Properties.Tags),
                        DownloadCount = version.GetScalar<int>(Uris.Properties.DownloadCount),
                        DependencySets = version.GetArray<JObject>(Uris.Properties.DependencyGroup).Select(obj => LoadDependencySet(obj))
                    };

                    string publishedStr = version.GetScalar<string>(Uris.Properties.Published);
                    if (!String.IsNullOrEmpty(publishedStr))
                    {
                        detailedPackage.Published = DateTime.Parse(publishedStr);
                    }
                    detailedPackage.NoDependencies = !HasDependencies(detailedPackage.DependencySets);

                    retValue.Add(detailedPackage);
                }

                return retValue;
            }

            private PackageDependencySet LoadDependencySet(JObject set)
            {
                var fxName = set.GetScalar<string>(Uris.Properties.TargetFramework);
                return new PackageDependencySet(
                    String.IsNullOrEmpty(fxName) ? null : VersionUtility.ParseFrameworkName(fxName),
                    set.GetArray<JObject>(Uris.Properties.Dependency).Select(obj => LoadDependency(obj)));
            }

            private PackageDependency LoadDependency(JObject dep)
            {
                var ver = dep.GetScalar<string>(Uris.Properties.VersionRange);
                return new PackageDependency(
                    dep.GetScalar<string>(Uris.Properties.PackageId),
                    String.IsNullOrEmpty(ver) ? null : VersionUtility.ParseVersionSpec(ver));
            }

            private bool HasDependencies(IEnumerable<PackageDependencySet> dependencySets)
            {
                if (dependencySets == null)
                {
                    return false;
                }

                foreach (var dependencySet in dependencySets)
                {
                    if (dependencySet.Dependencies != null &&
                        dependencySet.Dependencies.Count > 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            private string StringCollectionToString(IEnumerable<string> v)
            {
                if (v == null)
                {
                    return null;
                }

                string retValue = String.Join(", ", v);
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
            var optionsPageActivator = ServiceLocator.GetInstance<IOptionsPageActivator>();
            optionsPageActivator.ActivatePage(
                OptionsPage.PackageSources,
                null);
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
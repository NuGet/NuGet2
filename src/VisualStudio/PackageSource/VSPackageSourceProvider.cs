using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsPackageSourceProvider))]
    [Export(typeof(IPackageSourceProvider))]
    public class VsPackageSourceProvider : IVsPackageSourceProvider
    {
        private static readonly string OfficialFeedName = VsResources.OfficialSourceName;
        private static readonly string VisualStudioExpressForWindows8FeedName = VsResources.VisualStudioExpressForWindows8SourceName;
        private static readonly PackageSource _defaultSource = new PackageSource(NuGetConstants.DefaultFeedUrl, OfficialFeedName);
        private static readonly Dictionary<PackageSource, PackageSource> _feedsToMigrate = new Dictionary<PackageSource, PackageSource>
        {
            { new PackageSource(NuGetConstants.V1FeedUrl, OfficialFeedName), _defaultSource },
            { new PackageSource(NuGetConstants.V2LegacyFeedUrl, OfficialFeedName), _defaultSource },
        };
        internal const string ActivePackageSourceSectionName = "activePackageSource";
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly IVsShellInfo _vsShellInfo;
        private readonly ISettings _settings;
        private bool _initialized;
        private List<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        [ImportingConstructor]
        public VsPackageSourceProvider(
            ISettings settings,
            IVsShellInfo vsShellInfo) :
            this(settings, new PackageSourceProvider(settings, new[] { _defaultSource }, _feedsToMigrate), vsShellInfo)
        {
        }

        internal VsPackageSourceProvider(
            ISettings settings,
            IPackageSourceProvider packageSourceProvider,
            IVsShellInfo vsShellInfo)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (packageSourceProvider == null)
            {
                throw new ArgumentNullException("packageSourceProvider");
            }

            if (vsShellInfo == null)
            {
                throw new ArgumentNullException("vsShellInfo");
            }

            _packageSourceProvider = packageSourceProvider;
            _settings = settings;
            _vsShellInfo = vsShellInfo;
        }

        public PackageSource ActivePackageSource
        {
            get
            {
                EnsureInitialized();

                return _activePackageSource;
            }
            set
            {
                EnsureInitialized();

                if (value != null &&
                    !IsAggregateSource(value) &&
                    !_packageSources.Contains(value) &&
                    !value.Name.Equals(OfficialFeedName, StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new ArgumentException(VsResources.PackageSource_Invalid, "value");
                }

                _activePackageSource = value;

                PersistActivePackageSource(_settings, _vsShellInfo, _activePackageSource);
            }
        }

        internal static IEnumerable<PackageSource> DefaultSources
        {
            get { return new[] { _defaultSource }; }
        }

        internal static Dictionary<PackageSource, PackageSource> FeedsToMigrate
        {
            get { return _feedsToMigrate; }
        }

        public IEnumerable<PackageSource> LoadPackageSources()
        {
            EnsureInitialized();
            // assert that we are not returning aggregate source
            Debug.Assert(_packageSources == null || !_packageSources.Any(IsAggregateSource));
            return _packageSources;
        }

        public void SavePackageSources(IEnumerable<PackageSource> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException("sources");
            }

            EnsureInitialized();
            Debug.Assert(!sources.Any(IsAggregateSource));

            ActivePackageSource = null;
            _packageSources.Clear();
            _packageSources.AddRange(sources);

            PersistPackageSources(_packageSourceProvider, _vsShellInfo, _packageSources);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                _initialized = true;
                _packageSources = _packageSourceProvider.LoadPackageSources().ToList();

                // Unlike NuGet Core, Visual Studio has the concept of an official package source. 
                // We find the official source, if present, and set its IsOfficial it.
                var officialPackageSource = _packageSources.FirstOrDefault(packageSource => IsOfficialPackageSource(packageSource));
                if (officialPackageSource != null)
                {
                    officialPackageSource.IsOfficial = true;
                }

                // When running Visual Studio Express for Windows 8, we swap in a curated feed for the normal official feed.
                if (_vsShellInfo.IsVisualStudioExpressForWindows8)
                {
                    _packageSources = _packageSources.Select(packageSource => packageSource.IsOfficial ?
                        new PackageSource(NuGetConstants.VisualStudioExpressForWindows8FeedUrl, VisualStudioExpressForWindows8FeedName, isEnabled: packageSource.IsEnabled, isOfficial: true) :
                        packageSource)
                        .ToList();
                }

                InitializeActivePackageSource();
            }
        }

        private void InitializeActivePackageSource()
        {
            _activePackageSource = DeserializeActivePackageSource(_settings, _vsShellInfo);

            PackageSource migratedActiveSource;
            bool activeSourceChanged = false;
            if (_activePackageSource == null)
            {
                // If there are no sources, pick the first source that's enabled.
                activeSourceChanged = true;
                _activePackageSource = _defaultSource;
            }
            else if (_feedsToMigrate.TryGetValue(_activePackageSource, out migratedActiveSource))
            {
                // Check if we need to migrate the active source.
                activeSourceChanged = true;
                _activePackageSource = migratedActiveSource;
            }

            if (activeSourceChanged)
            {
                PersistActivePackageSource(_settings, _vsShellInfo, _activePackageSource);
            }
        }

        private static void PersistActivePackageSource(
            ISettings settings,
            IVsShellInfo vsShellInfo,
            PackageSource activePackageSource)
        {
            if (activePackageSource != null)
            {
                // When running Visual Studio Express For Windows 8, we will have previously swapped in a curated package source for the normal official source.
                // But, we always want to persist the normal official source. So, we swap the normal official source back in for the curated source.
                if (vsShellInfo.IsVisualStudioExpressForWindows8 && activePackageSource.IsOfficial)
                {
                    activePackageSource = new PackageSource(NuGetConstants.DefaultFeedUrl, OfficialFeedName, isEnabled: activePackageSource.IsEnabled, isOfficial: true);
                }

                settings.SetValue(ActivePackageSourceSectionName, activePackageSource.Name, activePackageSource.Source);
            }
            else
            {
                settings.DeleteSection(ActivePackageSourceSectionName);
            }
        }

        private static PackageSource DeserializeActivePackageSource(
            ISettings settings,
            IVsShellInfo vsShellInfo)
        {
            var settingValues = settings.GetValues(ActivePackageSourceSectionName);

            PackageSource packageSource = null;
            if (settingValues != null && settingValues.Any())
            {
                KeyValuePair<string, string> setting = settingValues.First();
                if (IsAggregateSource(setting.Key, setting.Value))
                {
                    packageSource = AggregatePackageSource.Instance;
                }
                else
                {
                    packageSource = new PackageSource(setting.Value, setting.Key);
                }
            }

            if (packageSource != null)
            {
                // guard against corrupted data if the active package source is not enabled
                packageSource.IsEnabled = true;

                // Unlike NuGet Core, Visual Studio has the concept of an official package source. 
                // If the active package source is the official source, we need to set its IsOfficial it.
                if (IsOfficialPackageSource(packageSource))
                {
                    packageSource.IsOfficial = true;

                    // When running Visual Studio Express for Windows 8, we swap in a curated feed for the normal official feed.
                    if (vsShellInfo.IsVisualStudioExpressForWindows8)
                    {
                        packageSource = new PackageSource(NuGetConstants.VisualStudioExpressForWindows8FeedUrl, VisualStudioExpressForWindows8FeedName, isEnabled: packageSource.IsEnabled, isOfficial: true);
                    }
                }
            }

            return packageSource;
        }

        private static void PersistPackageSources(IPackageSourceProvider packageSourceProvider, IVsShellInfo vsShellInfo, List<PackageSource> packageSources)
        {
            // When running Visual Studio Express For Windows 8, we will have previously swapped in a curated package source for the normal official source.
            // But, we always want to persist the normal official source. So, we swap the normal official source back in for the curated source.
            if (vsShellInfo.IsVisualStudioExpressForWindows8)
            {
                packageSources = packageSources.Select(packageSource => packageSource.IsOfficial ?
                    new PackageSource(NuGetConstants.DefaultFeedUrl, OfficialFeedName, isEnabled: packageSource.IsEnabled, isOfficial: true) :
                    packageSource)
                    .ToList();
            }

            // Starting from version 1.3, we persist the package sources to the nuget.config file instead of VS registry.
            // assert that we are not saving aggregate source
            Debug.Assert(!packageSources.Any(p => IsAggregateSource(p.Name, p.Source)));
            packageSourceProvider.SavePackageSources(packageSources);
        }

        private static bool IsAggregateSource(string name, string source)
        {
            PackageSource aggregate = AggregatePackageSource.Instance;
            return aggregate.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) ||
                aggregate.Source.Equals(source, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsAggregateSource(PackageSource packageSource)
        {
            return IsAggregateSource(packageSource.Name, packageSource.Source);
        }

        private static bool IsOfficialPackageSource(PackageSource packageSource)
        {
            if (packageSource == null)
            {
                return false;
            }

            return packageSource.Equals(_defaultSource);
        }
    }
}
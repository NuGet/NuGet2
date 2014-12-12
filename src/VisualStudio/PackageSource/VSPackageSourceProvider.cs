using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using NuGet.VisualStudio.Resources;
using System.Globalization;

namespace NuGet.VisualStudio
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsPackageSourceProvider))]
    [Export(typeof(IPackageSourceProvider))]
    public class VsPackageSourceProvider : IVsPackageSourceProvider
    {
        private static readonly string NuGetOfficialFeedNameV3 = VsResources.NuGetOfficialPreviewSourceName;
        private static readonly string NuGetLegacyOfficialFeedName = VsResources.NuGetLegacyOfficialSourceName;
        private static readonly string NuGetOfficialFeedName = VsResources.NuGetOfficialSourceName;
        private static readonly PackageSource NuGetDefaultSource = new PackageSource(NuGetConstants.DefaultFeedUrl, NuGetOfficialFeedName);
        private static readonly PackageSource NuGetV3Source = new PackageSource(
            NuGetConstants.V3FeedUrl, 
            NuGetOfficialFeedNameV3,
            isEnabled: true,
            isOfficial: true,
            isPersistable: false);
        
        private static readonly PackageSource Windows8Source = new PackageSource(
            NuGetConstants.VSExpressForWindows8FeedUrl,
            VsResources.VisualStudioExpressForWindows8SourceName,
            isEnabled: true,
            isOfficial: true,
            isPersistable: false);

        private static readonly Dictionary<PackageSource, PackageSource> _feedsToMigrate = new Dictionary<PackageSource, PackageSource>
        {
            { new PackageSource(NuGetConstants.V1FeedUrl, NuGetLegacyOfficialFeedName), NuGetDefaultSource },
            { new PackageSource(NuGetConstants.V2LegacyFeedUrl, NuGetLegacyOfficialFeedName), NuGetDefaultSource },
            { new PackageSource(NuGetConstants.V2LegacyOfficialPackageSourceUrl, NuGetLegacyOfficialFeedName), NuGetDefaultSource },
        };

        internal const string ActivePackageSourceSectionName = "activePackageSource";

        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly IVsShellInfo _vsShellInfo;
        private readonly ISettings _settings;
        private readonly ISolutionManager _solutionManager;
        private bool _initialized;
        private List<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        [ImportingConstructor]
        public VsPackageSourceProvider(
            ISettings settings,            
            IVsShellInfo vsShellInfo,
            ISolutionManager solutionManager) :
            this(settings, new PackageSourceProvider(settings, new[] { NuGetV3Source, NuGetDefaultSource }, _feedsToMigrate), vsShellInfo, solutionManager)
        {
        }

        internal VsPackageSourceProvider(
            ISettings settings,
            IPackageSourceProvider packageSourceProvider,
            IVsShellInfo vsShellInfo)
            :this(settings, packageSourceProvider, vsShellInfo, null)
        {
        }

        private VsPackageSourceProvider(
            ISettings settings,            
            IPackageSourceProvider packageSourceProvider,
            IVsShellInfo vsShellInfo,
            ISolutionManager solutionManager)
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
            _solutionManager = solutionManager;
            _settings = settings;
            _vsShellInfo = vsShellInfo;
            _packageSources = new List<PackageSource>();

            if (null != _solutionManager)
            {
                _solutionManager.SolutionClosed += OnSolutionOpenedOrClosed;
                _solutionManager.SolutionOpened += OnSolutionOpenedOrClosed;
            }
        }

        private void OnSolutionOpenedOrClosed(object sender, EventArgs e)
        {
            _initialized = false;
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
                    !_packageSources.Contains(value) &&
                    !value.Name.Equals(NuGetOfficialFeedName, StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new ArgumentException(VsResources.PackageSource_Invalid, "value");
                }

                _activePackageSource = value;

                PersistActivePackageSource(_settings, _activePackageSource);
            }
        }

        internal static IEnumerable<PackageSource> DefaultSources
        {
            get { return new[] { NuGetDefaultSource }; }
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

            // reload the sources
            _initialized = false;
            EnsureInitialized();
            var newSources = sources.ToList();
            Debug.Assert(!newSources.Any(IsAggregateSource));

            if (PackageSourcesEqual(_packageSources, newSources))
            {
                return;
            }
            
            ActivePackageSource = null;
            _packageSources.Clear();
            _packageSources.AddRange(newSources);

            PersistPackageSources(_packageSourceProvider, _vsShellInfo, _packageSources);

            if (PackageSourcesSaved != null)
            {
                PackageSourcesSaved(this, EventArgs.Empty);
            }
        }

        // We only need to check properties that can be changed by user through UI.
        internal static bool PackageSourcesEqual(List<PackageSource> a, List<PackageSource> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; ++i)
            {
                var s1 = a[i];
                var s2 = b[i];

                if (!StringComparer.CurrentCultureIgnoreCase.Equals(s1.Name, s2.Name))
                {
                    return false;
                }

                if (!StringComparer.OrdinalIgnoreCase.Equals(s1.Source, s2.Source))
                {
                    return false;
                }

                if (s1.IsEnabled != s2.IsEnabled)
                {
                    return false;
                }
            }

            return true;
        }

        public void DisablePackageSource(PackageSource source)
        {
            // There's no scenario for this method to get called, so do nothing here.
            Debug.Fail("This method shouldn't get called.");
        }

        public bool IsPackageSourceEnabled(PackageSource source)
        {
            EnsureInitialized();

            var sourceInUse = _packageSources.FirstOrDefault(ps => ps.Equals(source));
            return sourceInUse != null && sourceInUse.IsEnabled;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (this)
            {
                if (_initialized)
                {
                    return;
                }

                _packageSources = _packageSourceProvider.LoadPackageSources().ToList();

                // When running Visual Studio Express for Windows 8, we insert the curated feed at the top
                if (_vsShellInfo.IsVisualStudioExpressForWindows8)
                {
                    bool windows8SourceIsEnabled = _packageSourceProvider.IsPackageSourceEnabled(Windows8Source);

                    // defensive coding: make sure we don't add duplicated win8 source
                    _packageSources.RemoveAll(p => p.Equals(Windows8Source));

                    // Windows8Source is a static object which is meant for doing comparison only. 
                    // To add it to the list of package sources, we make a clone of it first.
                    var windows8SourceClone = Windows8Source.Clone();
                    windows8SourceClone.IsEnabled = windows8SourceIsEnabled;
                    _packageSources.Insert(0, windows8SourceClone);
                }

                InitializeActivePackageSource();
                _initialized = true;
            }
        }

        private void InitializeActivePackageSource()
        {
            _activePackageSource = DeserializeActivePackageSource(_settings, _vsShellInfo);

            bool activeSourceChanged = false;
            
            // Don't allow the aggregate source as the active source any more!
            if (IsAggregateSource(_activePackageSource))
            {
                activeSourceChanged = true;
                _activePackageSource = _packageSources.FirstOrDefault(p => p.IsEnabled);
            }

            PackageSource migratedActiveSource;
            if (_activePackageSource == null)
            {
                // If there are no sources, pick the first source that's enabled.
                activeSourceChanged = true;
                _activePackageSource = _packageSources.FirstOrDefault(p => p.IsEnabled);
            }
            else if (_feedsToMigrate.TryGetValue(_activePackageSource, out migratedActiveSource))
            {
                // Check if we need to migrate the active source.
                activeSourceChanged = true;
                _activePackageSource = migratedActiveSource;
            }

            if (activeSourceChanged)
            {
                PersistActivePackageSource(_settings, _activePackageSource);
            }
        }

        private static void PersistActivePackageSource(ISettings settings, PackageSource activePackageSource)
        {
            settings.DeleteSection(ActivePackageSourceSectionName);

            if (activePackageSource != null)
            {
                settings.SetValue(ActivePackageSourceSectionName, activePackageSource.Name, activePackageSource.Source);
            }
        }

        private PackageSource DeserializeActivePackageSource(
            ISettings settings,
            IVsShellInfo vsShellInfo)
        {
            // NOTE: Even though the aggregate source is being disabled, this method can still return it because
            //  it could be reading a v2.x active source setting. The caller of this method will migrate the active source
            //  to the first enabled feed.

            var enabledSources = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var source in _packageSources.Where(p => p.IsEnabled))
            {
                enabledSources.Add(source.Name);
            }

            // The special aggregate source, i.e. "All", is always enabled.
            enabledSources.Add(Resources.VsResources.AggregateSourceName);

            var settingValues = settings.GetValues(ActivePackageSourceSectionName);
            if (settingValues != null)
            {
                settingValues = settingValues.Where(s => enabledSources.Contains(s.Key)).ToList();
            }

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
            }

            return packageSource;
        }

        private static void PersistPackageSources(IPackageSourceProvider packageSourceProvider, IVsShellInfo vsShellInfo, List<PackageSource> packageSources)
        {
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
            return packageSource != null &&
                IsAggregateSource(packageSource.Name, packageSource.Source);
        }


        public event EventHandler PackageSourcesSaved;
    }
}
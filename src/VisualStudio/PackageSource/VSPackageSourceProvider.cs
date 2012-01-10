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
        private static readonly string OfficialFeedName = Resources.VsResources.OfficialSourceName;
        private static readonly PackageSource _defaultSource = new PackageSource(NuGetConstants.DefaultFeedUrl, OfficialFeedName);
        private static readonly Dictionary<PackageSource, PackageSource> _feedsToMigrate = new Dictionary<PackageSource, PackageSource>
        {
            { new PackageSource(NuGetConstants.V1FeedUrl, OfficialFeedName), _defaultSource },
            { new PackageSource(NuGetConstants.V2LegacyFeedUrl, OfficialFeedName), _defaultSource },
        };
        internal const string ActivePackageSourceSectionName = "activePackageSource";
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly ISettings _settings;
        private bool _initialized;
        private List<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        [ImportingConstructor]
        public VsPackageSourceProvider(ISettings settings) :
            this(settings, new PackageSourceProvider(settings, new[] { _defaultSource }, _feedsToMigrate))
        {
        }

        internal VsPackageSourceProvider(ISettings settings, IPackageSourceProvider packageSourceProvider)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (packageSourceProvider == null)
            {
                throw new ArgumentNullException("packageSourceProvider");
            }

            _packageSourceProvider = packageSourceProvider;
            _settings = settings;
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
                PersistActivePackageSource(_settings, _activePackageSource);
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
            Debug.Assert(_packageSources == null || !_packageSources.Any(p => IsAggregateSource(p)));
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

            PersistPackageSources(_packageSourceProvider, _packageSources);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                _initialized = true;
                _packageSources = _packageSourceProvider.LoadPackageSources().ToList();

                InitializeActivePackageSource();
            }
        }

        private void InitializeActivePackageSource()
        {
            _activePackageSource = DeserializeActivePackageSource(_settings);

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
                PersistActivePackageSource(_settings, _activePackageSource);
            }
        }
        
        private static void PersistActivePackageSource(ISettings settings, PackageSource activePackageSource)
        {
            if (activePackageSource != null)
            {
                settings.SetValue(ActivePackageSourceSectionName, activePackageSource.Name, activePackageSource.Source);
            }
            else
            {
                settings.DeleteSection(ActivePackageSourceSectionName);
            }
        }

        private static PackageSource DeserializeActivePackageSource(ISettings settings)
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
            }
            return packageSource;
        }

        private static void PersistPackageSources(IPackageSourceProvider packageSourceProvider, List<PackageSource> packageSources)
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
            return IsAggregateSource(packageSource.Name, packageSource.Source);
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsPackageSourceProvider))]
    [Export(typeof(IPackageSourceProvider))]
    public class VsPackageSourceProvider : IVsPackageSourceProvider {
        internal const string ActivePackageSourceSectionName = "activePackageSource";
        internal static readonly string OfficialFeedName = Resources.VsResources.OfficialSourceName;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly ISettings _settings;
        private List<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        [ImportingConstructor]
        public VsPackageSourceProvider(ISettings settings) :
            this(settings, new PackageSourceProvider(settings)) {
        }

        internal VsPackageSourceProvider(ISettings settings, IPackageSourceProvider packageSourceProvider) {

            if (settings == null) {
                throw new ArgumentNullException("settings");
            }

            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }

            _packageSourceProvider = packageSourceProvider;
            _settings = settings;
            _packageSources = _packageSourceProvider.LoadPackageSources().ToList();

            DeserializeActivePackageSource();
            AddOfficialPackageSourceIfNeeded();
            MigrateActivePackageSource();
        }

        public PackageSource ActivePackageSource {
            get {
                return _activePackageSource;
            }
            set {
                if (value != null && 
                    !IsAggregateSource(value) && 
                    !_packageSources.Contains(value) &&
                    !value.Name.Equals(OfficialFeedName, StringComparison.CurrentCultureIgnoreCase)) {
                    throw new ArgumentException(VsResources.PackageSource_Invalid, "value");
                }

                _activePackageSource = value;
                PersistActivePackageSource();
            }
        }

        public IEnumerable<PackageSource> LoadPackageSources() {
            // assert that we are not returning aggregate source
            Debug.Assert(_packageSources == null || !_packageSources.Any(p => IsAggregateSource(p)));
            return _packageSources;
        }

        internal void AddPackageSource(PackageSource source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (!_packageSources.Contains(source) && !IsAggregateSource(source)) {
                _packageSources.Add(source);

                // if the package source that we just added is the only one, make it the default
                if (ActivePackageSource == null && _packageSources.Count == 1) {
                    ActivePackageSource = source;
                }

                PersistPackageSources();
            }
        }

        internal bool RemovePackageSource(PackageSource source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            bool result = _packageSources.Remove(source);
            if (result) {
                PersistPackageSources();
                if (source.Equals(ActivePackageSource)) {
                    ActivePackageSource = null;
                }
            }

            return result;
        }

        public void SavePackageSources(IEnumerable<PackageSource> sources) {
            if (sources == null) {
                throw new ArgumentNullException("sources");
            }

            Debug.Assert(!sources.Any(p => IsAggregateSource(p)));

            ActivePackageSource = null;
            _packageSources.Clear();
            _packageSources.AddRange(sources.Where(p => !IsAggregateSource(p)));

            PersistPackageSources();
        }

        private void PersistPackageSources() {
            // Starting from version 1.3, we persist the package sources to the nuget.config file instead of VS registry.

            // assert that we are not saving aggregate source
            Debug.Assert(!_packageSources.Any(p => IsAggregateSource(p.Name, p.Source)));
            _packageSourceProvider.SavePackageSources(_packageSources);
        }

        private void PersistActivePackageSource() {
            // Starting from version 1.3, we persist the package sources to the nuget.config file instead of VS registry.
            _settings.DeleteSection(ActivePackageSourceSectionName);

            if (_activePackageSource != null) {
                _settings.SetValue(ActivePackageSourceSectionName, _activePackageSource.Name, _activePackageSource.Source);
            }
        }

        private void DeserializeActivePackageSource() {
            var settingValues = _settings.GetValues(ActivePackageSourceSectionName);

            PackageSource packageSource = null;
            if (settingValues != null && settingValues.Any()) {
                KeyValuePair<string, string> setting = settingValues.First();
                if (IsAggregateSource(setting.Key, setting.Value)) {
                    packageSource = AggregatePackageSource.Instance;
                }
                else {
                    packageSource = new PackageSource(setting.Value, setting.Key);
                }
            }

            if (packageSource != null) {
                // active package source must be enabled. 
                Debug.Assert(packageSource.IsEnabled);

                // guard against corrupted data if the active package source is not enabled
                packageSource.IsEnabled = true;

                ActivePackageSource = packageSource;
            }
        }

        private void MigrateActivePackageSource() {
            // migrate the active source from the V1 feed to the V2 feed if applicable
            if (ActivePackageSource != null &&
                ActivePackageSource.Source.Equals(NuGetConstants.V1FeedUrl, StringComparison.OrdinalIgnoreCase) &&
                ActivePackageSource.Name.Equals(OfficialFeedName, StringComparison.CurrentCultureIgnoreCase)) {

                ActivePackageSource = new PackageSource(NuGetConstants.DefaultFeedUrl, OfficialFeedName);
            }
        }

        private void AddOfficialPackageSourceIfNeeded() {
            // Look for an official source by name
            PackageSource officialFeed = _packageSources.FirstOrDefault(ps => ps.Name == OfficialFeedName);
            bool isOfficialFeedEnabled = true;

            if (officialFeed == null) {
                // There is no official feed currently registered

                // Don't register our feed unless the list is empty (other than the aggregate). This is the first-run scenario.
                // It also applies if user deletes all their feeds, in which case bringing back the official feed makes sense.
                if (_packageSources.Count > 1) {
                    return;
                }

            }
            else {
                // If there is an official feed that already points to the right place, we're done
                if (NuGetConstants.DefaultFeedUrl.Equals(officialFeed.Source, StringComparison.OrdinalIgnoreCase)) {
                    return;
                }

                // It doesn't point to the right place (e.g. came from previous build), so get rid of it
                RemovePackageSource(officialFeed);
                // in this case, we want to preserve the IsEnabled property of the official Feed
                isOfficialFeedEnabled = officialFeed.IsEnabled;
            }

            // Insert it at first position
            officialFeed = new PackageSource(NuGetConstants.DefaultFeedUrl, OfficialFeedName, isOfficialFeedEnabled);
            _packageSources.Insert(0, officialFeed);

            // Only make it the active source if there isn't one already
            if (ActivePackageSource == null) {
                ActivePackageSource = officialFeed;
            }

            PersistPackageSources();
        }

        private bool IsAggregateSource(string name, string source) {
            PackageSource aggregate = AggregatePackageSource.Instance;
            return aggregate.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) ||
                aggregate.Source.Equals(source, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsAggregateSource(PackageSource packageSource) {
            return IsAggregateSource(packageSource.Name, packageSource.Source);
        }
    }
}
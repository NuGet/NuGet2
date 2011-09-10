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
        internal const string FileSettingsActiveSectionName = "activePackageSource";
        internal static readonly string OfficialFeedName = Resources.VsResources.OfficialSourceName;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly ISettings _fileSettingsManager;
        private List<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        [ImportingConstructor]
        public VsPackageSourceProvider(ISettings settings) :
            this(settings, new PackageSourceProvider(settings)) {
        }

        internal VsPackageSourceProvider(ISettings fileSettingsManager, IPackageSourceProvider packageSourceProvider) {

            if (fileSettingsManager == null) {
                throw new ArgumentNullException("fileSettingsManager");
            }

            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }

            _packageSourceProvider = packageSourceProvider;
            _fileSettingsManager = fileSettingsManager;
            _packageSources = _packageSourceProvider.LoadPackageSources().ToList();
            
            DeserializeActivePackageSource();
            AddOfficialPackageSourceIfNeeded();
        }

        public PackageSource ActivePackageSource {
            get {
                return _activePackageSource;
            }
            set {
                if (value != null && !IsAggregateSource(value) && !_packageSources.Contains(value)) {
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
            _fileSettingsManager.DeleteSection(FileSettingsActiveSectionName);

            if (_activePackageSource != null) {
                _fileSettingsManager.SetValue(FileSettingsActiveSectionName, _activePackageSource.Name, _activePackageSource.Source);
            }
        }

        private void DeserializeActivePackageSource() {
            // try reading from the nuget.config file first
            var settingValues = _fileSettingsManager.GetValues(FileSettingsActiveSectionName);

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
                // this is to guard against corrupted VS user settings store
                AddPackageSource(packageSource);

                ActivePackageSource = packageSource;
            }
        }

        private void AddOfficialPackageSourceIfNeeded() {
            // Look for an official source by name
            PackageSource officialFeed = LoadPackageSources().FirstOrDefault(ps => ps.Name == OfficialFeedName);

            if (officialFeed == null) {

                // There is no official feed currently registered

                // Don't register our feed unless the list is empty (other than the aggregate). This is the first-run scenario.
                // It also applies if user deletes all their feeds, in which case bringing back the official feed makes sense.
                if (LoadPackageSources().Count() > 1) {
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
            }

            // Insert it at first position 
            officialFeed = new PackageSource(NuGetConstants.DefaultFeedUrl, OfficialFeedName);
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
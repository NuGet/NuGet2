using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsPackageSourceProvider))]
    [Export(typeof(IPackageSourceProvider))]
    public class VsPackageSourceProvider : IVsPackageSourceProvider {

        internal const string FileSettingsActiveSectionName = "activePackageSource";
        internal const string DefaultPackageSource = "https://go.microsoft.com/fwlink/?LinkID=206669";
        internal static readonly string OfficialFeedName = Resources.VsResources.OfficialSourceName;
        
        private readonly IPackageSourceSettingsManager _registrySettingsManager;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly ISettings _fileSettingsManager;

        private List<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        [ImportingConstructor]
        public VsPackageSourceProvider(IPackageSourceSettingsManager registrySettingsManager) : 
            this(registrySettingsManager, Settings.UserSettings, PackageSourceProvider.Default) {
        }

        internal VsPackageSourceProvider(
            IPackageSourceSettingsManager registrySettingsManager, 
            ISettings fileSettingsManager,
            IPackageSourceProvider packageSourceProvider) {

            if (fileSettingsManager == null) {
                throw new ArgumentNullException("fileSettingsManager");
            }

            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }

            _registrySettingsManager = registrySettingsManager;
            _packageSourceProvider = packageSourceProvider;
            _fileSettingsManager = fileSettingsManager;

            DeserializePackageSources();
            DeserializeActivePackageSource();
            AddOfficialPackageSourceIfNeeded();
        }

        public PackageSource ActivePackageSource {
            get {
                return _activePackageSource;
            }
            set {
                if (value != null && !_packageSources.Contains(value)) {
                    throw new ArgumentException(VsResources.PackageSource_Invalid, "value");
                }

                _activePackageSource = value;
                PersistActivePackageSource();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This method is potentially expensive because we are retrieving data from VS settings store.")]
        public IEnumerable<PackageSource> LoadPackageSources() {
            return _packageSources;
        }

        internal void AddPackageSource(PackageSource source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (!_packageSources.Contains(source)) {
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
            _packageSources.Clear();
            ActivePackageSource = null;

            if (sources != null) {
                foreach (var s in sources) {
                    _packageSources.Add(s);
                }
            }
            if (!_packageSources.Contains(AggregatePackageSource.Instance)) {
                _packageSources.Insert(0, AggregatePackageSource.Instance);
            }

            PersistPackageSources();
        }

        private void PersistPackageSources() {
            // Starting from version 1.3, we persist the package sources to the nuget.config file instead of VS registry.
            // don't persist aggregate source
            _packageSourceProvider.SavePackageSources(_packageSources.Where(p => !p.IsAggregate()));
        }

        private void PersistActivePackageSource() {
            // Starting from version 1.3, we persist the package sources to the nuget.config file instead of VS registry.
            _fileSettingsManager.DeleteSection(FileSettingsActiveSectionName);

            if (_activePackageSource != null) {
                _fileSettingsManager.SetValue(FileSettingsActiveSectionName, _activePackageSource.Name, _activePackageSource.Source);
            }
        }

        private void DeserializePackageSources() {
            LoadAndMigratePackageSources();

            if (_packageSources == null) {
                _packageSources = new List<PackageSource>();
                _packageSources.Add(AggregatePackageSource.Instance);
            }
            else if (!_packageSources.Any(AggregatePackageSource.IsAggregate)) {
                // look for a package source with the Source value as "(Aggregate source)"
                var index = _packageSources.FindIndex(ps => ps.Source.Equals(AggregatePackageSource.Instance.Source));
                if (index > -1) {
                    _packageSources[index] = AggregatePackageSource.Instance;
                }
                else {
                    // only add the aggregate source if there is not already one
                    _packageSources.Insert(0, AggregatePackageSource.Instance);
                }
            }
            else {
                for (int i = 0; i < _packageSources.Count; i++) {
                    var source = _packageSources[i];
                    // in 1.0, we didn't localize the All word. Fix it here opportunistically.
                    if (source.Name.Equals("All", StringComparison.CurrentCultureIgnoreCase)) {
                        _packageSources[i] = new PackageSource(source.Source, AggregatePackageSource.Instance.Name);
                    }
                }
            }
        }

        private void LoadAndMigratePackageSources() {
            // read from nuget.config first
            List<PackageSource> packageSourcesFromUserSettings = _packageSourceProvider.LoadPackageSources().ToList();
            if (packageSourcesFromUserSettings.Count > 0) {
                _packageSources = packageSourcesFromUserSettings;
            }
            else {
                string propertyString = _registrySettingsManager.PackageSourcesString;
                if (!String.IsNullOrEmpty(propertyString)) {
                    _packageSources = SerializationHelper.Deserialize<List<PackageSource>>(propertyString);

                    // if reading from VS registry, do the migration to nuget.config here
                    PersistPackageSources();

                    // delete the values in VS registry
                    _registrySettingsManager.PackageSourcesString = null;
                }
            }
        }

        private void DeserializeActivePackageSource() {
            // try reading from the nuget.config file first
            var settingValues = _fileSettingsManager.GetValues(FileSettingsActiveSectionName);

            PackageSource packageSource;
            if (settingValues != null && settingValues.Any()) {
                KeyValuePair<string, string> setting = settingValues.First();
                // if the active source is the Aggregate source, we persist it as <add key="All" value="(Aggregate source)" />
                if (AggregatePackageSource.Instance.Name.Equals(setting.Key, StringComparison.CurrentCultureIgnoreCase)) {
                    packageSource = AggregatePackageSource.Instance;
                }
                else {
                    packageSource = new PackageSource(setting.Value, setting.Key);
                }
            }
            else {
                // if reading from nuget settings file failed, fall back to reading from the VS registry
                packageSource = SerializationHelper.Deserialize<PackageSource>(_registrySettingsManager.ActivePackageSourceString);

                // and do the migration here, deleting the property in VS registry
                _registrySettingsManager.ActivePackageSourceString = null;
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
                if (DefaultPackageSource.Equals(officialFeed.Source, StringComparison.OrdinalIgnoreCase)) {
                    return;
                }

                // It doesn't point to the right place (e.g. came from previous build), so get rid of it
                RemovePackageSource(officialFeed);
            }

            // Insert it at position 1, so it is right after the aggregate
            officialFeed = new PackageSource(DefaultPackageSource, OfficialFeedName);
            _packageSources.Insert(1, officialFeed);

            // Only make it the active source if there isn't one already
            if (ActivePackageSource == null) {
                ActivePackageSource = officialFeed;
            }

            PersistPackageSources();
        }
    }
}
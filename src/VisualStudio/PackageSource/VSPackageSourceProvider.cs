using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPackageSourceProvider))]
    public class VsPackageSourceProvider : IPackageSourceProvider {

        internal const string FileSettingsSectionName = "packageSources";
        internal const string FileSettingsActiveSectionName = "activePackageSource";

        private const string AggregateSourceValue = "(Aggregate source)";
        internal const string DefaultPackageSource = "https://go.microsoft.com/fwlink/?LinkID=206669";
        internal static readonly string OfficialFeedName = Resources.VsResources.OfficialSourceName;
        private static readonly PackageSource AggregateSourceInstance = new PackageSource("(Aggregate source)", Resources.VsResources.AggregateSourceName) { IsAggregate = true };
        
        private readonly ISettings _fileSettingsManager;

        private readonly IPackageSourceSettingsManager _registrySettingsManager;

        private List<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        [ImportingConstructor]
        public VsPackageSourceProvider(IPackageSourceSettingsManager registrySettingsManager) : 
            this(registrySettingsManager, Settings.UserSettings) {
        }

        internal VsPackageSourceProvider(IPackageSourceSettingsManager registrySettingsManager, ISettings fileSettingsManager) {
            if (fileSettingsManager == null) {
                throw new ArgumentNullException("fileSettingsManager");
            }

            _registrySettingsManager = registrySettingsManager;
            _fileSettingsManager = fileSettingsManager;

            DeserializePackageSources();
            DeserializeActivePackageSource();
            AddOfficialPackageSourceIfNeeded();
        }

        public PackageSource AggregateSource {
            get {
                return AggregateSourceInstance;
            }
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
        public IEnumerable<PackageSource> GetPackageSources() {
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

        public void SetPackageSources(IEnumerable<PackageSource> sources) {
            _packageSources.Clear();
            ActivePackageSource = null;

            if (sources != null) {
                foreach (var s in sources) {
                    _packageSources.Add(s);
                }
            }
            if (!_packageSources.Contains(AggregateSourceInstance)) {
                _packageSources.Insert(0, AggregateSourceInstance);
            }

            PersistPackageSources();
        }

        private void PersistPackageSources() {
            // Starting from version 1.3, we persist the package sources to the nuget.config file instead of VS registry.

            // clear the old values
            _fileSettingsManager.DeleteSection(FileSettingsSectionName);

            // and write the new ones
            // don't persist aggregate source
            _fileSettingsManager.SetValues(
                FileSettingsSectionName, 
                _packageSources.Where(p => !p.IsAggregate).Select(p => new KeyValuePair<string, string>(p.Name, p.Source)).ToList());
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
                _packageSources.Add(AggregateSourceInstance);
            }
            else if (!_packageSources.Any(ps => ps.IsAggregate)) {
                // look for a package source with the Source value as "(Aggregate source)"
                var source = _packageSources.FirstOrDefault(ps => ps.Source.Equals(AggregateSourceValue));
                if (source != null) {
                    source.IsAggregate = true;
                }
                else {
                    // only add the aggregate source if there is not already one
                    _packageSources.Insert(0, AggregateSource);
                }
            }
            else {
                // Try to detect if there are more than one aggreage sources.
                // This should never happen, but just guard against it to avoid crashing VS.
                bool seenAggregate = false;
                for (int i = 0; i < _packageSources.Count; i++) {
                    var source = _packageSources[i];
                    if (source.IsAggregate) {
                        if (seenAggregate) {
                            source.IsAggregate = false;
                        }
                        else {
                            seenAggregate = true;

                            // in 1.0, we didn't localize the All word. Fix it here opportunistically.
                            if (source.Name.Equals("All", StringComparison.CurrentCultureIgnoreCase)) {
                                _packageSources[i] = new PackageSource(source.Source, AggregateSource.Name) { IsAggregate = true };
                            }
                        }
                    }
                }
            }
        }

        private void LoadAndMigratePackageSources() {
            // read from nuget.config first
            IList<KeyValuePair<string, string>> settingsValue = _fileSettingsManager.GetValues(FileSettingsSectionName);
            if (settingsValue != null && settingsValue.Count > 0) {
                _packageSources = settingsValue.Select(p => new PackageSource(p.Value, p.Key)).ToList();
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
                if (AggregateSourceInstance.Name.Equals(setting.Key, StringComparison.CurrentCultureIgnoreCase)) {
                    packageSource = AggregateSourceInstance;
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
            PackageSource officialFeed = GetPackageSources().FirstOrDefault(ps => ps.Name == OfficialFeedName);

            if (officialFeed == null) {

                // There is no official feed currently registered

                // Don't register our feed unless the list is empty (other than the aggregate). This is the first-run scenario.
                // It also applies if user deletes all their feeds, in which case bringing back the official feed makes sense.
                if (GetPackageSources().Count() > 1) {
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
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPackageSourceProvider))]
    public class VsPackageSourceProvider : IPackageSourceProvider {
        internal const string DefaultPackageSource = "http://go.microsoft.com/fwlink/?LinkID=206669";
        internal const string OfficialFeedName = "NuGet official package source";
        internal static readonly PackageSource AggregateSource = new PackageSource("(Aggregate source)", "All") { IsAggregate = true };

        private readonly IPackageSourceSettingsManager _settingsManager;

        private List<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        [ImportingConstructor]
        public VsPackageSourceProvider(IPackageSourceSettingsManager settingsManager) {
            _settingsManager = settingsManager;

            DeserializePackageSources();
            AddOfficialPackageSourceIfNeeded();
            DeserializeActivePackageSource();
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

                // persist the value into VS settings store
                _settingsManager.ActivePackageSourceString = SerializationHelper.Serialize(value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This method is potentially expensive because we are retrieving data from VS settings store.")]
        public IEnumerable<PackageSource> GetPackageSources() {
            return _packageSources;
        }

        public void AddPackageSource(PackageSource source) {
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

        public bool RemovePackageSource(PackageSource source) {
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

            PersistPackageSources();
        }

        private void PersistPackageSources() {
            _settingsManager.PackageSourcesString = SerializationHelper.Serialize(_packageSources);
        }

        private void DeserializePackageSources() {
            string propertyString = _settingsManager.PackageSourcesString;

            if (!String.IsNullOrEmpty(propertyString)) {
                _packageSources = SerializationHelper.Deserialize<List<PackageSource>>(propertyString);
            }

            if (_packageSources == null) {
                _packageSources = new List<PackageSource>();
                _packageSources.Add(AggregateSource);
            }
            else if (!_packageSources.Contains(AggregateSource)) {
                _packageSources.Insert(0, AggregateSource);
            }
        }

        private void AddOfficialPackageSourceIfNeeded() {
            // Look for a source with the name of the official source
            PackageSource officialFeed = GetPackageSources().FirstOrDefault(ps => ps.Name == OfficialFeedName);

            if (officialFeed != null) {
                // If there is one and it points to the right place, we're done
                if (officialFeed.Source == DefaultPackageSource) return;

                // It doesn't point to the right place (e.g. came from previous build), do get rid of it
                RemovePackageSource(officialFeed);
            }

            // Add the official source to the list.  This covers 3 scenarios:
            // 1. First time NuGet is ever run, so there is nothing
            // 2. NuGet ran before, but official feed is missing. We bring it back with the assumtion that it should always be in the list.
            //    Otherwise they may not be able to get it back if it was mistakenly deleted.
            // 3. It's an upgrade from on older build which had a different default feed
            officialFeed = new PackageSource(DefaultPackageSource, OfficialFeedName);
            AddPackageSource(officialFeed);

            ActivePackageSource = officialFeed;
        }

        private void DeserializeActivePackageSource() {
            var packageSource = SerializationHelper.Deserialize<PackageSource>(_settingsManager.ActivePackageSourceString);
            if (packageSource != null) {
                // this is to guard against corrupted VS user settings store
                AddPackageSource(packageSource);

                ActivePackageSource = packageSource;
            }
        }
    }
}

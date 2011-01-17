using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {


    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export("RecentPackagesRepository", typeof(IPackageRepository))]
    public class RecentPackagesRepository : IPackageRepository {

        private const string SourceValue = "(MRU)";
        private const int MaximumPackageCount = 10;

        private readonly List<IPersistencePackageMetadata> _packagesMetadata;
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPersistencePackageSettingsManager _settingsManager;
        private bool _hasLoadedSettingsStore;
        private DTEEvents _dteEvents;
        private Dictionary<IPersistencePackageMetadata, IPackage> _packagesCache;

        [ImportingConstructor]
        public RecentPackagesRepository(
            DTE dte,
            IPackageRepositoryFactory repositoryFactory, 
            IPersistencePackageSettingsManager settingsManager) {

            _repositoryFactory = repositoryFactory;
            _settingsManager = settingsManager;
            _packagesMetadata = new List<IPersistencePackageMetadata>();

            if (dte != null) {
                _dteEvents = dte.Events.DTEEvents;
                _dteEvents.OnBeginShutdown += OnBeginShutdown;
            }
        }

        public string Source {
            get {
                return SourceValue;
            }
        }

        public IQueryable<IPackage> GetPackages() {
            EnsurePackagesFromSettingsStoreLoaded();
            
            IEnumerable<IPersistencePackageMetadata> packagesMetadata = _packagesMetadata.Take(MaximumPackageCount);
            IList<IPackage> validPackages = new List<IPackage>();

            // filter out invalid packages (packages which can't be retrieved from the source)
            foreach (var metadata in packagesMetadata) {
                IPackage package = GetPackageFromMetadata(metadata);
                if (package != null) {
                    validPackages.Add(package);
                }
            }

            return validPackages.AsQueryable();
        }

        public void AddPackage(IPackage package) {
            var packageMetadata = package as IPersistencePackageMetadata;
            if (packageMetadata == null) {
                throw new ArgumentException(VsResources.PackageCanNotBePersisted, "package");
            }

            AddPackageMetadata(packageMetadata);
        }

        public void RemovePackage(IPackage package) {
            throw new NotSupportedException();
        }

        private void AddPackageMetadata(IPersistencePackageMetadata metadata) {
            var index = _packagesMetadata.FindIndex(p => PackageMetadataEqualityComparer.Instance.Equals(metadata, p));
            if (index >= 0) {
                // if this package is already in the Recent list, then move it to the front
                metadata = _packagesMetadata[index];
                _packagesMetadata.RemoveAt(index);
            }

            // make sure we are not persist the package source as '(MRU)'
            Debug.Assert(metadata.Source != SourceValue);

            // add the package to the front of the list to simulate a stack
            _packagesMetadata.Insert(0, metadata);
        }

        /// <summary>
        /// Look up the cache first, otherwise, create a new package instance
        /// </summary>
        private IPackage GetPackageFromMetadata(IPersistencePackageMetadata metadata) {
            IPackage package;
            if (!_packagesCache.TryGetValue(metadata, out package)) {
                try {
                    package = new RecentPackage(metadata, _repositoryFactory);
                }
                catch (ArgumentException) {
                    package = null;
                }
                _packagesCache.Add(metadata, package);
            }

            return package;
        }

        private void EnsurePackagesFromSettingsStoreLoaded() {
            if (_hasLoadedSettingsStore) {
                return;
            }

            foreach (var metadata in _settingsManager.LoadPackageMetadata(MaximumPackageCount)) {
                AddPackageMetadata(metadata);
            }

            // Used to cache the created packages so that we don't have to make requests every time the MRU list is accessed
            _packagesCache = new Dictionary<IPersistencePackageMetadata, IPackage>(PackageMetadataEqualityComparer.Instance);

            _hasLoadedSettingsStore = true;
        }

        private void SavePackagesToSettingsStore() {
            // only save if there are new package metadata added
            if (_packagesMetadata.Count > 0) {
                EnsurePackagesFromSettingsStoreLoaded();
                _settingsManager.SavePackageMetadata(_packagesMetadata.Take(MaximumPackageCount));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void OnBeginShutdown() {
            _dteEvents.OnBeginShutdown -= OnBeginShutdown;

            try {
                // save recent packages to settings store before IDE shuts down
                SavePackagesToSettingsStore();
            }
            catch (Exception) {
                // we don't care if the saving fails.
            }
        }

        /// <summary>
        /// Comparer which compares two IPersistencePackageMetadata on Id and Version. Source is NOT considered.
        /// </summary>
        private class PackageMetadataEqualityComparer : IEqualityComparer<IPersistencePackageMetadata> {

            public static readonly PackageMetadataEqualityComparer Instance = new PackageMetadataEqualityComparer();

            public bool Equals(IPersistencePackageMetadata x, IPersistencePackageMetadata y) {
                return x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase) && x.Version == y.Version;
            }

            public int GetHashCode(IPersistencePackageMetadata obj) {
                return obj.Id.GetHashCode() * 3137 + obj.Version.GetHashCode();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;

namespace NuGet.VisualStudio {

    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IRecentPackageRepository))]
    public class RecentPackagesRepository : IPackageRepository, IRecentPackageRepository {

        private const string SourceValue = "(MRU)";
        private const int MaximumPackageCount = 20;

        private readonly List<IPackage> _packages;
        private readonly Dictionary<PersistencePackageMetadata, IPackage> _packagesCache;
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPersistencePackageSettingsManager _settingsManager;
        private readonly DTEEvents _dteEvents;
        private bool _hasLoadedSettingsStore;

        [ImportingConstructor]
        public RecentPackagesRepository(
            DTE dte,
            IPackageRepositoryFactory repositoryFactory,
            IPersistencePackageSettingsManager settingsManager) {

            _packages = new List<IPackage>();
            _repositoryFactory = repositoryFactory;
            _settingsManager = settingsManager;

            // Used to cache the created packages so that we don't have to make requests every time the MRU list is accessed
            _packagesCache = new Dictionary<PersistencePackageMetadata, IPackage>();

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
            LoadPackagesFromSettingsStore();
            return _packages.Take(MaximumPackageCount).AsQueryable();
        }

        public void AddPackage(IPackage package) {
            var packageMetadata = new PersistencePackageMetadata(package);
            AddPackage(packageMetadata, package, addToFront: true);
        }

        /// <summary>
        /// Add the specified package to the list.
        /// </summary>
        /// <param name="addToFront">if set to true, it will add package to the front</param>
        private void AddPackage(PersistencePackageMetadata metadata, IPackage package, bool addToFront) {
            var index = _packages.FindIndex(p => metadata.Equals(p));
            if (index >= 0) {
                if (addToFront) {
                    package = _packages[index];
                    _packages.RemoveAt(index);
                }
                else {
                    return;
                }
            }

            if (addToFront) {
                _packages.Insert(0, package);
            }
            else {
                _packages.Add(package);
            }

            // also add it to the cache so that we don't have to retrieve the package again
            _packagesCache[metadata] = package;
        }

        public void RemovePackage(IPackage package) {
            throw new NotSupportedException();
        }

        public void Clear() {
            _packages.Clear();
            _settingsManager.ClearPackageMetadata();
        }

        private IEnumerable<PersistencePackageMetadata> LoadPackageMetadataFromSettingsStore() {
            // don't bother to load the settings store if we have loaded before or we already have enough packages in-memory
            if (_packages.Count >= MaximumPackageCount || _hasLoadedSettingsStore) {
                return Enumerable.Empty<PersistencePackageMetadata>();
            }

            _hasLoadedSettingsStore = true;

            return _settingsManager.LoadPackageMetadata(MaximumPackageCount - _packages.Count);
        }

        private void LoadPackagesFromSettingsStore() {

            // find recent packages from the Aggregate repository
            var aggregateRepository = _repositoryFactory.CreateRepository(VsPackageSourceProvider.AggregateSource);

            // for packages not in the cache, find them from the Aggregate repository based on Id only
            var packagesMetadata = LoadPackageMetadataFromSettingsStore();
            var newPackages = aggregateRepository.
                                FindPackages(packagesMetadata.Where(m => !_packagesCache.ContainsKey(m)).Select(p => p.Id));

            // newPackages contains all versions of a package Id. Filter out the versions that we don't care.
            var filterPackages = FilterPackages(packagesMetadata, newPackages);

            var cachedPackages = packagesMetadata.Where(m => _packagesCache.ContainsKey(m)).Select(m => Tuple.Create(m, _packagesCache[m]));

            var allPackages = filterPackages.Concat(cachedPackages);

            foreach (var p in allPackages) {
                AddPackage(p.Item1, p.Item2, addToFront: false);
            }
        }

        /// <summary>
        /// Select packages from 'allPackages' which match the Ids and Versions from packagesMetadata.
        /// Returns the result as a list of Tuple of (metadata, package)
        /// </summary>
        private static IEnumerable<Tuple<PersistencePackageMetadata, IPackage>> FilterPackages(
            IEnumerable<PersistencePackageMetadata> packagesMetadata,
            IEnumerable<IPackage> allPackages) {

            var lookup = packagesMetadata.ToLookup(p => p.Id, StringComparer.OrdinalIgnoreCase);

            return from p in allPackages
                   where lookup.Contains(p.Id)
                   let m = lookup[p.Id].FirstOrDefault(m => m.Version == p.Version)
                   where m != null
                   select Tuple.Create(m, p);
        }

        private void SavePackagesToSettingsStore() {
            // only save if there are new package added
            if (_packages.Count > 0) {

                // IMPORTANT: call ToList() here. Otherwise, we may read and write to the settings store at the same time
                var loadedPacakgesMetadata = LoadPackageMetadataFromSettingsStore().ToList();

                _settingsManager.SavePackageMetadata(
                    _packages.
                        Take(MaximumPackageCount).
                        Cast<PersistencePackageMetadata>().
                        Concat(loadedPacakgesMetadata));
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
    }
}
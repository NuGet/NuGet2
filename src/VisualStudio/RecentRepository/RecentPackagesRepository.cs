using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EnvDTE;

namespace NuGet.VisualStudio {

    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IRecentPackageRepository))]
    public class RecentPackageRepository : IPackageRepository, IRecentPackageRepository {

        private const string SourceValue = "(MRU)";
        private const int MaximumPackageCount = 20;

        /// <remarks>
        /// The cache would be small enough for us to not care about iterating over large lists.
        /// </remarks>
        private readonly List<RecentPackage> _packagesCache = new List<RecentPackage>(20);
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPersistencePackageSettingsManager _settingsManager;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly DTEEvents _dteEvents;
        private bool _hasLoadedSettingsStore;
        private List<PackageSource> _currentSources;
        private DateTime _latestTime = DateTime.UtcNow;

        [ImportingConstructor]
        public RecentPackageRepository(IPackageRepositoryFactory repositoryFactory,
                                        IPersistencePackageSettingsManager settingsManager)
            : this(ServiceLocator.GetInstance<DTE>(),
                   repositoryFactory,
                   ServiceLocator.GetInstance<IPackageSourceProvider>(),
                   settingsManager) {
        }

        public RecentPackageRepository(
            DTE dte,
            IPackageRepositoryFactory repositoryFactory,
            IPackageSourceProvider packageSourceProvider,
            IPersistencePackageSettingsManager settingsManager) {

            _packageSourceProvider = packageSourceProvider;
            _repositoryFactory = repositoryFactory;
            _settingsManager = settingsManager;

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
            // Create an instance of the Aggregate repository
            var repository = _packageSourceProvider.GetAggregate(_repositoryFactory, ignoreFailingRepositories: true);

            LoadPackagesFromSettingsStore(repository);

            if (PackageSourcesChanged()) {
                // If package sources have changes since the last call, ensure that at least some version of it is available on a feed.
                UpdatePackageCache(repository);
            }

            // IMPORTANT: The Cast() operator is needed to return IQueryable<IPackage> instead of IQueryable<RecentPackage>.
            // Although the compiler accepts the latter, the DistinctLast() method chokes on it.
            return _packagesCache
                    .OrderByDescending(p => p.LastUsedDate)
                    .Take(MaximumPackageCount)
                    .Cast<IPackage>()
                    .AsQueryable();
        }

        public void AddPackage(IPackage package) {
            AddRecentPackage(ConvertToRecentPackage(package, GetUniqueTime()));
        }

        private DateTime GetUniqueTime() {
            // This guarantees all the DateTime values are unique. We don't care what the actual value is.
            _latestTime = _latestTime.AddSeconds(1);
            return _latestTime;
        }

        /// <summary>
        /// Add the specified package to the list.
        /// </summary>
        private void AddRecentPackage(RecentPackage package) {
            var index = _packagesCache.FindIndex(p => p.Id == package.Id);
            if (index != -1) {
                var cachedPackage = _packagesCache[index];
                if (package.Version > cachedPackage.Version) {
                    _packagesCache[index] = package;
                }
                _packagesCache[index].LastUsedDate = (package.LastUsedDate > cachedPackage.LastUsedDate) ?
                    package.LastUsedDate : cachedPackage.LastUsedDate;
            }
            else {
                _packagesCache.Add(package);
            }
        }

        private static RecentPackage ConvertToRecentPackage(IPackage package, DateTime lastUsedDate) {
            var recentPackage = package as RecentPackage;
            if (recentPackage != null) {
                // if the package is already an instance of RecentPackage, reset the date and return it
                recentPackage.LastUsedDate = lastUsedDate;
                return recentPackage;
            }
            else {
                // otherwise, wrap it inside a RecentPackage
                return new RecentPackage(package, lastUsedDate);
            }
        }

        public void RemovePackage(IPackage package) {
            throw new NotSupportedException();
        }

        public void Clear() {
            _packagesCache.Clear();
            _settingsManager.ClearPackageMetadata();
        }

        private IEnumerable<IPersistencePackageMetadata> LoadPackageMetadataFromSettingsStore() {
            // don't bother to load the settings store if we have loaded before or we already have enough packages in-memory
            if (_packagesCache.Count >= MaximumPackageCount || _hasLoadedSettingsStore) {
                return Enumerable.Empty<PersistencePackageMetadata>();
            }

            _hasLoadedSettingsStore = true;

            return _settingsManager.LoadPackageMetadata(MaximumPackageCount - _packagesCache.Count);
        }

        private void LoadPackagesFromSettingsStore(IPackageRepository repository) {
            // get the metadata of recent packages from registry
            IEnumerable<IPersistencePackageMetadata> packagesMetadata = LoadPackageMetadataFromSettingsStore();

            // find the packages based on metadata from the Aggregate repository based on Id only
            IEnumerable<IPackage> newPackages = repository.FindPackages(packagesMetadata.Select(p => p.Id));

            // newPackages contains all versions of a package Id. Filter out the versions that we don't care.
            IEnumerable<RecentPackage> filterPackages = FilterPackages(packagesMetadata, newPackages);
            foreach (var package in filterPackages) {
                AddRecentPackage(package);
            }
        }

        /// <summary>
        /// Select packages from 'allPackages' which match the Ids and Versions from packagesMetadata.
        /// </summary>
        private static IEnumerable<RecentPackage> FilterPackages(
            IEnumerable<IPersistencePackageMetadata> packagesMetadata,
            IEnumerable<IPackage> allPackages) {

            var lookup = packagesMetadata.ToLookup(p => p.Id, StringComparer.OrdinalIgnoreCase);

            return from p in allPackages
                   where lookup.Contains(p.Id)
                   let m = lookup[p.Id].FirstOrDefault(m => m.Version == p.Version)
                   where m != null
                   select ConvertToRecentPackage(p, m.LastUsedDate);
        }

        /// <summary>
        /// Updates the package cache by ensuring all cached packages exist in the repository.
        /// </summary>
        private void UpdatePackageCache(IPackageRepository repository) {
            var feedPackages = repository.FindPackages(_packagesCache.Select(p => p.Id)).ToLookup(p => p.Id, StringComparer.OrdinalIgnoreCase);
            _packagesCache.RemoveAll(p => !feedPackages.Contains(p.Id));
        }


        private void SavePackagesToSettingsStore() {
            // only save if there are new package added
            if (_packagesCache.Count > 0) {

                // IMPORTANT: call ToList() here. Otherwise, we may read and write to the settings store at the same time
                var loadedPackagesMetadata = LoadPackageMetadataFromSettingsStore().ToList();

                _settingsManager.SavePackageMetadata(
                    _packagesCache.
                        Take(MaximumPackageCount).
                        Concat(loadedPackagesMetadata));
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void OnBeginShutdown() {
            _dteEvents.OnBeginShutdown -= OnBeginShutdown;

            try {
                // save recent packages to settings store before IDE shuts down
                SavePackagesToSettingsStore();
            }
            catch (Exception exception) {
                // write to activity log for troubleshoting.
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        /// <summary>
        /// Determines if package sources have changed since the last time we queried.
        /// </summary>
        private bool PackageSourcesChanged() {
            var sources = _packageSourceProvider.LoadPackageSources().ToList();

            if (_currentSources == null) {
                // The package cache has just been read from settings and is in sync with the sources. 
                _currentSources = sources;
                return false;
            }

            var union = _currentSources.Union(sources);
            var intersection = _currentSources.Intersect(sources);
            // (A ∪ B) - (A ∩ B)
            if (union.Except(intersection).Any()) {
                _currentSources = sources;
                return true;
            }
            return false;
        }
    }
}
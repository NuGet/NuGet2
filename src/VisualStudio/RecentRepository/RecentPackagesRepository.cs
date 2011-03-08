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

        private readonly HashSet<RecentPackage> _packagesCache = new HashSet<RecentPackage>();
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPersistencePackageSettingsManager _settingsManager;
        private readonly DTEEvents _dteEvents;
        private readonly PackageSource _aggregatePackageSource;
        private bool _hasLoadedSettingsStore;
        private DateTime _latestTime = DateTime.UtcNow;

        [ImportingConstructor]
        public RecentPackagesRepository(IPackageRepositoryFactory repositoryFactory,
                                        IPersistencePackageSettingsManager settingsManager)
            : this(ServiceLocator.GetInstance<DTE>(), repositoryFactory, ServiceLocator.GetInstance<IPackageSourceProvider>(), settingsManager) {
        }

        public RecentPackagesRepository(
            DTE dte,
            IPackageRepositoryFactory repositoryFactory,
            IPackageSourceProvider packageSourceProvider,
            IPersistencePackageSettingsManager settingsManager) {

            _repositoryFactory = repositoryFactory;
            _settingsManager = settingsManager;
            _aggregatePackageSource = packageSourceProvider.ActivePackageSource;

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
            // IMPORTANT: The Cast() operator is needed to return IQueryable<IPackage> instead of IQueryable<RecentPackage>.
            // Although the compiler accepts the latter, the DistinctLast() method chokes on it.
            return _packagesCache.
                OrderByDescending(p => p.LastUsedDate).
                Take(MaximumPackageCount).
                Cast<IPackage>().
                AsQueryable();
        }

        public void AddPackage(IPackage package) {
            AddRecentPackage(ConvertToRecentPackage(package, GetUniqueTime()), true);
        }

        private DateTime GetUniqueTime() {
            // This guarantees all the DateTime values are unique. We don't care what the actual value is.
            _latestTime = _latestTime.AddSeconds(1);
            return _latestTime;
        }

        /// <summary>
        /// Add the specified package to the list.
        /// </summary>
        private void AddRecentPackage(RecentPackage package, bool replaceExistingItem) {
            if (_packagesCache.Contains(package)) {
                if (replaceExistingItem) {
                    _packagesCache.Remove(package);
                }
                else {
                    return;
                }
            }

            _packagesCache.Add(package);
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

        private void LoadPackagesFromSettingsStore() {
            // find recent packages from the Aggregate repository
            IPackageRepository aggregateRepository = _repositoryFactory.CreateRepository(_aggregatePackageSource);

            // get the metadata of recent packages from registry
            IEnumerable<IPersistencePackageMetadata> packagesMetadata = LoadPackageMetadataFromSettingsStore();

            // find the packages based on metadata from the Aggregate repository based on Id only
            IEnumerable<IPackage> newPackages = aggregateRepository.FindPackages(packagesMetadata.Select(p => p.Id));

            // newPackages contains all versions of a package Id. Filter out the versions that we don't care.
            IEnumerable<RecentPackage> filterPackages = FilterPackages(packagesMetadata, newPackages);
            foreach (var p in filterPackages) {
                AddRecentPackage(p, false);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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
    }
}
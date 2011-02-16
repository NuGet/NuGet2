using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {

    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IRecentPackageRepository))]
    public class RecentPackagesRepository : IPackageRepository, IRecentPackageRepository {

        private const string SourceValue = "(MRU)";
        private const int MaximumPackageCount = 20;

        private readonly List<IPackage> _packages;
        private readonly Dictionary<IPersistencePackageMetadata, IPackage> _packagesCache;
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPersistencePackageSettingsManager _settingsManager;
        private readonly DTEEvents _dteEvents;
        private bool _hasLoadedSettingsStore;

        [ImportingConstructor]
        public RecentPackagesRepository(IPackageRepositoryFactory repositoryFactory,
                                        IPersistencePackageSettingsManager settingsManager)
            : this(ServiceLocator.GetInstance<DTE>(), repositoryFactory, settingsManager) {
        }

        public RecentPackagesRepository(
            DTE dte,
            IPackageRepositoryFactory repositoryFactory,
            IPersistencePackageSettingsManager settingsManager) {

            _packages = new List<IPackage>();
            _repositoryFactory = repositoryFactory;
            _settingsManager = settingsManager;

            // Used to cache the created packages so that we don't have to make requests every time the MRU list is accessed
            _packagesCache = new Dictionary<IPersistencePackageMetadata, IPackage>(PackageMetadataEqualityComparer.Instance);

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
            var packageMetadata = package as IPersistencePackageMetadata;
            if (packageMetadata == null) {
                throw new ArgumentException(VsResources.PackageCanNotBePersisted, "package");
            }

            AddPackage(packageMetadata, package, addToFront: true);
        }

        /// <summary>
        /// Add the specified package to the list.
        /// </summary>
        /// <param name="addToFront">if set to true, it will add package to the front</param>
        private void AddPackage(IPersistencePackageMetadata metadata, IPackage package, bool addToFront) {
            var index = _packages.FindIndex(p => PackageMetadataEqualityComparer.Instance.Equals(metadata, (IPersistencePackageMetadata)p));
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

        private IEnumerable<IPersistencePackageMetadata> LoadPackageMetadataFromSettingsStore() {
            // don't bother to load the settings store if we have loaded before or we already have enough packages in-memory
            if (_packages.Count >= MaximumPackageCount || _hasLoadedSettingsStore) {
                return Enumerable.Empty<IPersistencePackageMetadata>();
            }

            _hasLoadedSettingsStore = true;

            return _settingsManager.LoadPackageMetadata(MaximumPackageCount - _packages.Count);
        }

        private void LoadPackagesFromSettingsStore() {
            var packagesMetadata = LoadPackageMetadataFromSettingsStore();

            // for packages not in the cache, group them by sources, and get all packages with the matching Ids from each source
            var newPackages = packagesMetadata.
                                Where(m => !_packagesCache.ContainsKey(m)).
                                GroupBy(p => p.Source).
                                SelectMany(g => FindPackagesFromSource(g.Key, g).Select(p => new RecentPackage(p, g.Key)));

            // newPackages contains all versions of a package Id. Filter out the versions that we don't care.
            var filterPackages = FilterPackages(packagesMetadata, newPackages);

            foreach (var p in filterPackages) {
                AddPackage(p.Item1, p.Item2, addToFront: false);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private IEnumerable<IPackage> FindPackagesFromSource(string source, IEnumerable<IPersistencePackageMetadata> metadata) {
            var packageSource = new PackageSource(source);

            // HACK: be careful with the aggreate source. in which case, the source is a pseudo-source, e.g. '(Aggregate Source)'
            if (source.Equals(VsPackageSourceProvider.AggregateSource.Source, StringComparison.OrdinalIgnoreCase)) {
                packageSource.IsAggregate = true;
            }

            IPackageRepository repository = null;
            try {
                repository = _repositoryFactory.CreateRepository(packageSource);
            }
            catch (Exception) {
                // we don't care if the source value is invalid or corrupted, which 
                // will cause CreateRepository to throw.
            }

            return repository == null ?
                Enumerable.Empty<IPackage>() :
                repository.FindPackages(metadata.Select(m => m.Id).Distinct());
        }

        /// <summary>
        /// Select packages from 'allPackages' which match the Ids and Versions from packagesMetadata.
        /// Returns the result as a list of Tuple of (metadata, package)
        /// </summary>
        private static IEnumerable<Tuple<IPersistencePackageMetadata, IPackage>> FilterPackages(
            IEnumerable<IPersistencePackageMetadata> packagesMetadata,
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
                        Cast<IPersistencePackageMetadata>().
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
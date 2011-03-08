using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPackageRepositoryFactory))]
    public class CachedRepositoryFactory : IPackageRepositoryFactory {
        private readonly ConcurrentDictionary<PackageSource, IPackageRepository> _repositoryCache = new ConcurrentDictionary<PackageSource, IPackageRepository>();
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly IProgressReporter _progressReporter;

        [ImportingConstructor]
        public CachedRepositoryFactory(IPackageSourceProvider packageSourceProvider,
                                       IProgressReporter progressReporter)
            : this(PackageRepositoryFactory.Default,
                   progressReporter,
                   packageSourceProvider) {
        }

        internal CachedRepositoryFactory(IPackageRepositoryFactory repositoryFactory,
                                         IProgressReporter progressReporter,
                                         IPackageSourceProvider packageSourceProvider) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }

            if (progressReporter == null) {
                throw new ArgumentNullException("progressReporter");
            }

            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }

            _progressReporter = progressReporter;
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        public IPackageRepository CreateRepository(PackageSource packageSource) {
            if (packageSource.IsAggregate) {
                // Never cache the aggregate
                return new AggregateRepository(_packageSourceProvider.GetPackageSources()
                                                                     .Where(source => !source.IsAggregate)
                                                                     .Select(GetPackageRepository));
            }

            return GetPackageRepository(packageSource);
        }

        private IPackageRepository GetPackageRepository(PackageSource packageSource) {
            IPackageRepository repository;
            if (!_repositoryCache.TryGetValue(packageSource, out repository)) {
                repository = _repositoryFactory.CreateRepository(packageSource);
                _repositoryCache.TryAdd(packageSource, repository);

                // See if this repository provides progress
                var progressProvider = repository as IProgressProvider;
                if (progressProvider != null) {
                    progressProvider.ProgressAvailable += OnProgressAvailable;
                }
            }
            return repository;
        }

        private void OnProgressAvailable(object sender, ProgressEventArgs e) {
            _progressReporter.ReportProgress(e.Operation, e.PercentComplete);
        }
    }
}

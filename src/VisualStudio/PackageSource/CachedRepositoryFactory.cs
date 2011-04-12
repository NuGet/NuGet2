using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPackageRepositoryFactory))]
    [Export(typeof(IProgressProvider))]
    [Export(typeof(IHttpClientEvents))]
    public class CachedRepositoryFactory : IPackageRepositoryFactory, IProgressProvider, IHttpClientEvents {
        private readonly ConcurrentDictionary<PackageSource, IPackageRepository> _repositoryCache = new ConcurrentDictionary<PackageSource, IPackageRepository>();
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _packageSourceProvider;

        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };
        public event EventHandler<WebRequestEventArgs> SendingRequest = delegate { };

        [ImportingConstructor]
        public CachedRepositoryFactory(IPackageSourceProvider packageSourceProvider)
            : this(PackageRepositoryFactory.Default, packageSourceProvider) {
        }

        internal CachedRepositoryFactory(IPackageRepositoryFactory repositoryFactory,
                                         IPackageSourceProvider packageSourceProvider) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }

            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }

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

                var httpEvents = repository as IHttpClientEvents;
                if (httpEvents != null) {
                    httpEvents.SendingRequest += OnSendingRequest;
                }
            }
            return repository;
        }

        private void OnProgressAvailable(object sender, ProgressEventArgs e) {
            ProgressAvailable(this, e);           
        }

        private void OnSendingRequest(object sender, WebRequestEventArgs e) {
            SendingRequest(this, e);
        }
    }
}
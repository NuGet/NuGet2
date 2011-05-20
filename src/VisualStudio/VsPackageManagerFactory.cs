using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsPackageManagerFactory))]
    public class VsPackageManagerFactory : IVsPackageManagerFactory {
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly ISolutionManager _solutionManager;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IRepositorySettings _repositorySettings;
        private readonly IRecentPackageRepository _recentPackageRepository;
        private readonly IVsPackageSourceProvider _packageSourceProvider;

        private RepositoryInfo _repositoryInfo;

        [ImportingConstructor]
        public VsPackageManagerFactory(ISolutionManager solutionManager,
                                       IPackageRepositoryFactory repositoryFactory,
                                       IVsPackageSourceProvider packageSourceProvider,
                                       IFileSystemProvider fileSystemProvider,
                                       IRepositorySettings repositorySettings,
                                       IRecentPackageRepository recentPackagesRepository) {
            if (solutionManager == null) {
                throw new ArgumentNullException("solutionManager");
            }
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }
            if (fileSystemProvider == null) {
                throw new ArgumentNullException("fileSystemProvider");
            }
            if (repositorySettings == null) {
                throw new ArgumentNullException("repositorySettings");
            }

            _fileSystemProvider = fileSystemProvider;
            _repositorySettings = repositorySettings;
            _solutionManager = solutionManager;
            _repositoryFactory = repositoryFactory;
            _recentPackageRepository = recentPackagesRepository;
            _packageSourceProvider = packageSourceProvider;

            _solutionManager.SolutionClosing += (sender, e) => {
                _repositoryInfo = null;
            };
        }

        public IVsPackageManager CreatePackageManager() {
            return CreatePackageManager(ServiceLocator.GetInstance<IPackageRepository>());
        }

        public IVsPackageManager CreatePackageManager(IPackageRepository repository) {
            var fallbackRepository = CreateFallBackRepository(repository);
            RepositoryInfo info = GetRepositoryInfo();
            return new VsPackageManager(_solutionManager, fallbackRepository, info.FileSystem, info.Repository, _recentPackageRepository);
        }

        /// <summary>
        /// Creates a FallBackRepository with an aggregate repository that also constains the primaryRepository.
        /// </summary>
        internal IPackageRepository CreateFallBackRepository(IPackageRepository primaryRepository) {
            if (AggregatePackageSource.Instance.Source.Equals(primaryRepository.Source, StringComparison.OrdinalIgnoreCase)) {
                // If we're using the aggregate repository, we don't need to create a fall back repo.
                return primaryRepository;
            }

            var sources = _packageSourceProvider.LoadPackageSources().ToList();
            IEnumerable<IPackageRepository> repositories = sources.Select(c => _repositoryFactory.CreateRepository(c.Source));

            // We need to ensure that the primary repository is part of the aggregate repository. This could happen if the user
            // explicitly specifies a source such as by using the -Source parameter.
            if (!sources.Any(s => s.Source.Equals(primaryRepository.Source, StringComparison.OrdinalIgnoreCase))) {
                repositories = new[] { primaryRepository }.Concat(repositories);
            }
            var aggregateRepository = new AggregateRepository(repositories);
            return new FallbackRepository(primaryRepository, aggregateRepository);
        }

        private RepositoryInfo GetRepositoryInfo() {
            // Update the path if it needs updating
            string path = _repositorySettings.RepositoryPath;

            if (_repositoryInfo == null || !_repositoryInfo.Path.Equals(path)) {
                IFileSystem fileSystem = _fileSystemProvider.GetFileSystem(path);
                ISharedPackageRepository repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem);

                _repositoryInfo = new RepositoryInfo(path, fileSystem, repository);
            }

            return _repositoryInfo;
        }

        private class RepositoryInfo {
            public RepositoryInfo(string path, IFileSystem fileSystem, ISharedPackageRepository repository) {
                Path = path;
                FileSystem = fileSystem;
                Repository = repository;
            }

            public IFileSystem FileSystem { get; private set; }
            public string Path { get; private set; }
            public ISharedPackageRepository Repository { get; private set; }
        }
    }
}
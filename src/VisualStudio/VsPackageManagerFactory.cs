using System;
using System.ComponentModel.Composition;

namespace NuGet.VisualStudio
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsPackageManagerFactory))]
    public class VsPackageManagerFactory : IVsPackageManagerFactory
    {
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly ISolutionManager _solutionManager;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IRepositorySettings _repositorySettings;
        private readonly IRecentPackageRepository _recentPackageRepository;
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly VsPackageInstallerEvents _packageEvents;
        private readonly IPackageRepository _cacheRepository;

        private RepositoryInfo _repositoryInfo;

        [ImportingConstructor]
        public VsPackageManagerFactory(ISolutionManager solutionManager,
                                       IPackageRepositoryFactory repositoryFactory,
                                       IVsPackageSourceProvider packageSourceProvider,
                                       IFileSystemProvider fileSystemProvider,
                                       IRepositorySettings repositorySettings,
                                       IRecentPackageRepository recentPackagesRepository,
                                       VsPackageInstallerEvents packageEvents) :
            this(solutionManager, repositoryFactory, packageSourceProvider, fileSystemProvider, repositorySettings, recentPackagesRepository, packageEvents, MachineCache.Default)
        {
        }

        public VsPackageManagerFactory(ISolutionManager solutionManager,
                                       IPackageRepositoryFactory repositoryFactory,
                                       IVsPackageSourceProvider packageSourceProvider,
                                       IFileSystemProvider fileSystemProvider,
                                       IRepositorySettings repositorySettings,
                                       IRecentPackageRepository recentPackagesRepository,
                                       VsPackageInstallerEvents packageEvents,
                                       IPackageRepository cacheRepository)
        {
            if (solutionManager == null)
            {
                throw new ArgumentNullException("solutionManager");
            }
            if (repositoryFactory == null)
            {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (packageSourceProvider == null)
            {
                throw new ArgumentNullException("packageSourceProvider");
            }
            if (fileSystemProvider == null)
            {
                throw new ArgumentNullException("fileSystemProvider");
            }
            if (repositorySettings == null)
            {
                throw new ArgumentNullException("repositorySettings");
            }
            if (cacheRepository == null)
            {
                throw new ArgumentNullException("cacheRepository");
            }

            if (packageEvents == null)
            {
                throw new ArgumentNullException("packageEvents");
            }

            _fileSystemProvider = fileSystemProvider;
            _repositorySettings = repositorySettings;
            _solutionManager = solutionManager;
            _repositoryFactory = repositoryFactory;
            _recentPackageRepository = recentPackagesRepository;
            _packageSourceProvider = packageSourceProvider;
            _packageEvents = packageEvents;
            _cacheRepository = cacheRepository;

            _solutionManager.SolutionClosing += (sender, e) =>
            {
                _repositoryInfo = null;
            };
        }

        /// <summary>
        /// Creates an VsPackageManagerInstance that uses the Active Repository (the repository selected in the console drop down) and uses a fallback repository for dependencies.
        /// </summary>
        public IVsPackageManager CreatePackageManager()
        {
            return CreatePackageManager(ServiceLocator.GetInstance<IPackageRepository>(), useFallbackForDependencies: true);
        }

        public IVsPackageManager CreatePackageManager(IPackageRepository repository, bool useFallbackForDependencies)
        {
            return CreatePackageManager(repository, useFallbackForDependencies, addToRecent: true);
        }

        public IVsPackageManager CreatePackageManager(IPackageRepository repository, bool useFallbackForDependencies, bool addToRecent)
        {
            if (useFallbackForDependencies)
            {
                repository = CreateFallbackRepository(repository);
            }
            RepositoryInfo info = GetRepositoryInfo();
            return new VsPackageManager(_solutionManager,
                                        repository,
                                        info.FileSystem,
                                        info.Repository,
                                        _recentPackageRepository,
                                        _packageEvents,
                                        _cacheRepository)
                                        {
                                            AddToRecent = addToRecent
                                        };
        }

        /// <summary>
        /// Creates a FallbackRepository with an aggregate repository that also contains the primaryRepository.
        /// </summary>
        internal IPackageRepository CreateFallbackRepository(IPackageRepository primaryRepository)
        {
            if (IsAggregateRepository(primaryRepository))
            {
                // If we're using the aggregate repository, we don't need to create a fall back repo.
                return primaryRepository;
            }

            var aggregateRepository = _packageSourceProvider.GetAggregate(_repositoryFactory, ignoreFailingRepositories: true);
            aggregateRepository.ResolveDependenciesVertically = true;
            return new FallbackRepository(primaryRepository, aggregateRepository);
        }

        private static bool IsAggregateRepository(IPackageRepository repository)
        {
            if (repository is AggregateRepository)
            {
                // This test should be ok as long as any aggregate repository that we encounter here means the true Aggregate repository of all repositories in the package source
                // Since the repository created here comes from the UI, this holds true.
                return true;
            }
            var vsPackageSourceRepository = repository as VsPackageSourceRepository;
            if (vsPackageSourceRepository != null)
            {
                return IsAggregateRepository(vsPackageSourceRepository.ActiveRepository);
            }
            return false;
        }

        private RepositoryInfo GetRepositoryInfo()
        {
            // Update the path if it needs updating
            string path = _repositorySettings.RepositoryPath;

            if (_repositoryInfo == null || !_repositoryInfo.Path.Equals(path))
            {
                IFileSystem fileSystem = _fileSystemProvider.GetFileSystem(path);
                ISharedPackageRepository repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem);

                _repositoryInfo = new RepositoryInfo(path, fileSystem, repository);
            }

            return _repositoryInfo;
        }

        private class RepositoryInfo
        {
            public RepositoryInfo(string path, IFileSystem fileSystem, ISharedPackageRepository repository)
            {
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
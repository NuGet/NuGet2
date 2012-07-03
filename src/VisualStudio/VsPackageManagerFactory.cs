using System;
using System.ComponentModel.Composition;
using EnvDTE;

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
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly VsPackageInstallerEvents _packageEvents;

        private RepositoryInfo _repositoryInfo;

        [ImportingConstructor]
        public VsPackageManagerFactory(ISolutionManager solutionManager,
                                       IPackageRepositoryFactory repositoryFactory,
                                       IVsPackageSourceProvider packageSourceProvider,
                                       IFileSystemProvider fileSystemProvider,
                                       IRepositorySettings repositorySettings,
                                       VsPackageInstallerEvents packageEvents)
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
            if (packageEvents == null)
            {
                throw new ArgumentNullException("packageEvents");
            }

            _fileSystemProvider = fileSystemProvider;
            _repositorySettings = repositorySettings;
            _solutionManager = solutionManager;
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            _packageEvents = packageEvents;

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
            if (useFallbackForDependencies)
            {
                repository = CreateFallbackRepository(repository);
            }
            RepositoryInfo info = GetRepositoryInfo();
            return new VsPackageManager(_solutionManager,
                                        repository,
                                        _fileSystemProvider,
                                        info.FileSystem,
                                        info.Repository,
                                        _packageEvents);
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
                return IsAggregateRepository(vsPackageSourceRepository.GetActiveRepository());
            }
            return false;
        }

        private RepositoryInfo GetRepositoryInfo()
        {
            // Update the path if it needs updating
            string path = _repositorySettings.RepositoryPath;
            string configFolderPath = _repositorySettings.ConfigFolderPath;

            if (_repositoryInfo == null || 
                !_repositoryInfo.Path.Equals(path, StringComparison.OrdinalIgnoreCase) ||
                !_repositoryInfo.ConfigFolderPath.Equals(configFolderPath, StringComparison.OrdinalIgnoreCase) ||
                _solutionManager.IsSourceControlBound != _repositoryInfo.IsSourceControlBound)
            {
                IFileSystem fileSystem = _fileSystemProvider.GetFileSystem(path);
                IFileSystem configSettingsFileSystem = GetConfigSettingsFileSystem(configFolderPath);
                ISharedPackageRepository repository = new SharedPackageRepository(
                    new DefaultPackagePathResolver(fileSystem), fileSystem, configSettingsFileSystem);

                _repositoryInfo = new RepositoryInfo(path, configFolderPath, fileSystem, repository);
            }

            return _repositoryInfo;
        }

        protected internal virtual IFileSystem GetConfigSettingsFileSystem(string configFolderPath)
        {
            return new SolutionFolderFileSystem(ServiceLocator.GetInstance<DTE>().Solution, VsConstants.NuGetSolutionSettingsFolder, configFolderPath);
        }

        private class RepositoryInfo
        {
            public RepositoryInfo(string path, string configFolderPath, IFileSystem fileSystem, ISharedPackageRepository repository)
            {
                Path = path;
                FileSystem = fileSystem;
                Repository = repository;
                ConfigFolderPath = configFolderPath;
            }

            public bool IsSourceControlBound
            {
                get
                {
                    return FileSystem is ISourceControlFileSystem;
                }
            }

            public IFileSystem FileSystem { get; private set; }
            public string Path { get; private set; }
            public string ConfigFolderPath { get; private set; }
            public ISharedPackageRepository Repository { get; private set; }
        }
    }
}
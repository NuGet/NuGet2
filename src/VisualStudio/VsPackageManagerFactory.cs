using System;
using System.ComponentModel.Composition;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsPackageManagerFactory))]
    public class VsPackageManagerFactory : IVsPackageManagerFactory {
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly ISolutionManager _solutionManager;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IRepositorySettings _repositorySettings;
        private readonly IRecentPackageRepository _recentPackageRepository;

        private RepositoryInfo _repositoryInfo;

        [ImportingConstructor]
        public VsPackageManagerFactory(ISolutionManager solutionManager,
                                       IPackageRepositoryFactory repositoryFactory,
                                       IFileSystemProvider fileSystemProvider,
                                       IRepositorySettings repositorySettings,
                                       IRecentPackageRepository recentPackagesRepository) {
            if (solutionManager == null) {
                throw new ArgumentNullException("solutionManager");
            }
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
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

            _solutionManager.SolutionClosing += (sender, e) => {
                _repositoryInfo = null;
            };

        }

        public IVsPackageManager CreatePackageManager() {
            return CreatePackageManager(ServiceLocator.GetInstance<IPackageRepository>());
        }

        public IVsPackageManager CreatePackageManager(string source) {
            return CreatePackageManager(_repositoryFactory.CreateRepository(source));
        }

        public IVsPackageManager CreatePackageManager(IPackageRepository repository) {
            RepositoryInfo info = GetRepositoryInfo();

            return new VsPackageManager(_solutionManager, repository, info.FileSystem, info.Repository, _recentPackageRepository);
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
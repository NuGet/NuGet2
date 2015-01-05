using System;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

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
        private readonly IPackageRepository _activePackageSourceRepository;
        private readonly IVsFrameworkMultiTargeting _frameworkMultiTargeting;
        private RepositoryInfo _repositoryInfo;
		private readonly IMachineWideSettings _machineWideSettings;

        [ImportingConstructor]
        public VsPackageManagerFactory(ISolutionManager solutionManager,
                                       IPackageRepositoryFactory repositoryFactory,
                                       IVsPackageSourceProvider packageSourceProvider,
                                       IFileSystemProvider fileSystemProvider,
                                       IRepositorySettings repositorySettings,
                                       VsPackageInstallerEvents packageEvents,
                                       IPackageRepository activePackageSourceRepository,
									   IMachineWideSettings machineWideSettings) :
            this(solutionManager, 
                 repositoryFactory, 
                 packageSourceProvider, 
                 fileSystemProvider, 
                 repositorySettings, 
                 packageEvents,
                 activePackageSourceRepository,
                 ServiceLocator.GetGlobalService<SVsFrameworkMultiTargeting, IVsFrameworkMultiTargeting>(),
				 machineWideSettings)
        {
        }

        public VsPackageManagerFactory(ISolutionManager solutionManager,
                                       IPackageRepositoryFactory repositoryFactory,
                                       IVsPackageSourceProvider packageSourceProvider,
                                       IFileSystemProvider fileSystemProvider,
                                       IRepositorySettings repositorySettings,
                                       VsPackageInstallerEvents packageEvents,
                                       IPackageRepository activePackageSourceRepository,
                                       IVsFrameworkMultiTargeting frameworkMultiTargeting,
									   IMachineWideSettings machineWideSettings)
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
            if (activePackageSourceRepository == null)
            {
                throw new ArgumentNullException("activePackageSourceRepository");
            }

            _fileSystemProvider = fileSystemProvider;
            _repositorySettings = repositorySettings;
            _solutionManager = solutionManager;
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            _packageEvents = packageEvents;
            _activePackageSourceRepository = activePackageSourceRepository;
            _frameworkMultiTargeting = frameworkMultiTargeting;
			_machineWideSettings = machineWideSettings;

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
            return CreatePackageManager(_activePackageSourceRepository, useFallbackForDependencies: true);
        }

        // TODO: fallback repository is removed. Update the documentation.

        /// <summary>
        /// Creates a VsPackageManager that is used to manage install packages.
        /// The local repository is used as the primary source, and other active sources are 
        /// used as fall back repository. When all needed packages are available in the local 
        /// repository, which is the normal case, this package manager will not need to query
        /// any remote sources at all. Other active sources are 
        /// used as fall back repository so that it still works even if user has used 
        /// install-package -IgnoreDependencies.        
        /// </summary>
        /// <returns>The VsPackageManager created.</returns>
        public IVsPackageManager CreatePackageManagerToManageInstalledPackages()
        {
            RepositoryInfo info = GetRepositoryInfo();
            var aggregateRepository = _packageSourceProvider.CreateAggregateRepository(
                _repositoryFactory, ignoreFailingRepositories: true);
            aggregateRepository.ResolveDependenciesVertically = true;
            return CreatePackageManager(aggregateRepository, useFallbackForDependencies: false);
        }

        public IVsPackageManager CreatePackageManager(IPackageRepository repository, bool useFallbackForDependencies)
        {
            RepositoryInfo info = GetRepositoryInfo();
            var packageManager = new VsPackageManager(_solutionManager,
                                        repository,
                                        _fileSystemProvider,
                                        info.FileSystem,
                                        info.Repository,
                                        // We ensure DeleteOnRestartManager is initialized with a PhysicalFileSystem so the
                                        // .deleteme marker files that get created don't get checked into version control
                                        new DeleteOnRestartManager(() => new PhysicalFileSystem(info.FileSystem.Root)),
                                        _packageEvents,
                                        _frameworkMultiTargeting);
            packageManager.DependencyVersion = GetDependencyVersion();
            return packageManager;
        }

        public IVsPackageManager CreatePackageManagerWithAllPackageSources()
        {
            return CreatePackageManagerWithAllPackageSources(_activePackageSourceRepository);
        }

        internal IVsPackageManager CreatePackageManagerWithAllPackageSources(IPackageRepository repository)
        {
            if (IsAggregateRepository(repository))
            {
               return CreatePackageManager(repository, false);
            }

            var priorityRepository = _packageSourceProvider.CreatePriorityPackageRepository(_repositoryFactory, repository);
            return CreatePackageManager(priorityRepository, useFallbackForDependencies: false);
        }

        // TODO: fallback repository is removed. Update the documentation.

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

            var aggregateRepository = _packageSourceProvider.CreateAggregateRepository(_repositoryFactory, ignoreFailingRepositories: true);
            aggregateRepository.ResolveDependenciesVertically = true;
            return aggregateRepository;
        }

        private static bool IsAggregateRepository(IPackageRepository repository)
        {
            if (repository is AggregateRepository)
            {
                // This test should be ok as long as any aggregate repository that we encounter here means the true Aggregate repository 
                // of all repositories in the package source.
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
                // this file system is used to access the repositories.config file. We want to use Source Control-bound 
                // file system to access it even if the 'disableSourceControlIntegration' setting is set.
                IFileSystem storeFileSystem = _fileSystemProvider.GetFileSystem(path, ignoreSourceControlSetting: true);
                
                ISharedPackageRepository repository = new SharedPackageRepository(
                    new DefaultPackagePathResolver(fileSystem), 
                    fileSystem, 
                    storeFileSystem, 
                    configSettingsFileSystem);

                var settings = Settings.LoadDefaultSettings(
					configSettingsFileSystem, 
					configFileName: null, 
					machineWideSettings: _machineWideSettings);
                repository.PackageSaveMode = CalculatePackageSaveMode(settings);
                _repositoryInfo = new RepositoryInfo(path, configFolderPath, fileSystem, repository);
            }

            return _repositoryInfo;
        }

        /// <summary>
        /// Returns the user specified DependencyVersion in nuget.config.
        /// </summary>
        /// <returns>The user specified DependencyVersion value in nuget.config.</returns>
        private DependencyVersion GetDependencyVersion()
        {
            string configFolderPath = _repositorySettings.ConfigFolderPath;
            IFileSystem configSettingsFileSystem = GetConfigSettingsFileSystem(configFolderPath);
            var settings = Settings.LoadDefaultSettings(
                    configSettingsFileSystem,
                    configFileName: null,
                    machineWideSettings: _machineWideSettings);
            string dependencyVersionValue = settings.GetConfigValue("DependencyVersion");
            DependencyVersion dependencyVersion;
            if (Enum.TryParse(dependencyVersionValue, ignoreCase: true, result:out dependencyVersion))
            {
                return dependencyVersion;
            }
            else
            {
                return DependencyVersion.Lowest;
            }
        }

        private PackageSaveModes CalculatePackageSaveMode(ISettings settings)
        {
            PackageSaveModes retValue = PackageSaveModes.None;
            if (settings != null)
            {
                string packageSaveModeValue = settings.GetConfigValue("PackageSaveMode");
                // TODO: remove following block of code when shipping NuGet version post 2.8
                if (string.IsNullOrEmpty(packageSaveModeValue))
                {
                    packageSaveModeValue = settings.GetConfigValue("SaveOnExpand");
                }
                // end of block of code to remove when shipping NuGet version post 2.8
                if (!string.IsNullOrEmpty(packageSaveModeValue))
                {
                    foreach (var v in packageSaveModeValue.Split(';'))
                    {
                        if (v.Equals(PackageSaveModes.Nupkg.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            retValue |= PackageSaveModes.Nupkg;
                        }
                        else if (v.Equals(PackageSaveModes.Nuspec.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            retValue |= PackageSaveModes.Nuspec;
                        }
                    }
                }
            }

            if (retValue == PackageSaveModes.None)
            {
                retValue = PackageSaveModes.Nupkg;
            }

            return retValue;
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
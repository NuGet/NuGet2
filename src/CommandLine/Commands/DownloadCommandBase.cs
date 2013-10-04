using System.Collections.Generic;
using NuGet.Common;

namespace NuGet.Commands
{
    public abstract class DownloadCommandBase : Command
    {
        private readonly IPackageRepository _cacheRepository;
        private readonly List<string> _sources = new List<string>();

        protected DownloadCommandBase(IPackageRepository cacheRepository)
        {
            _cacheRepository = cacheRepository;
        }

        [Option(typeof(NuGetCommand), "CommandSourceDescription")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option(typeof(NuGetCommand), "CommandNoCache")]
        public bool NoCache { get; set; }

        [Option(typeof(NuGetCommand), "CommandDisableParallelProcessing")]
        public bool DisableParallelProcessing { get; set; }

        /// <remarks>
        /// Meant for unit testing.
        /// </remarks>
        protected IPackageRepository CacheRepository
        {
            get { return _cacheRepository; }
        }

        protected IPackageRepository CreateRepository()
        {
            AggregateRepository repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;

            if (NoCache)
            {
                return repository;
            }
            else
            {
                return new PriorityPackageRepository(CacheRepository, repository);
            }
        }

        protected virtual IPackageManager CreatePackageManager(IFileSystem packagesFolderFileSystem, bool useSideBySidePaths)
        {
            var repository = CreateRepository();
            var pathResolver = new DefaultPackagePathResolver(packagesFolderFileSystem, useSideBySidePaths);
            IPackageRepository localRepository = new LocalPackageRepository(pathResolver, packagesFolderFileSystem);
            var packageManager = new PackageManager(repository, pathResolver, packagesFolderFileSystem, localRepository)
            {
                Logger = Console
            };

            return packageManager;
        }
    }
}

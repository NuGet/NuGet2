using System;
using System.Collections.Generic;
using System.Globalization;
using NuGet.Common;

namespace NuGet.Commands
{
    public abstract class DownloadCommandBase : Command
    {
        private readonly IPackageRepository _cacheRepository;
        private readonly List<string> _sources = new List<string>();

        protected PackageSaveModes EffectivePackageSaveMode { get; set; }

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

        [Option(typeof(NuGetCommand), "CommandPackageSaveMode")]
        public string PackageSaveMode { get; set; }
        
        protected void CalculateEffectivePackageSaveMode()
        {
            string packageSaveModeValue = PackageSaveMode;
            if (string.IsNullOrEmpty(packageSaveModeValue))
            {
                packageSaveModeValue = Settings.GetConfigValue("PackageSaveMode");
            }

            EffectivePackageSaveMode = PackageSaveModes.None;
            if (!string.IsNullOrEmpty(packageSaveModeValue))
            {
                foreach (var v in packageSaveModeValue.Split(';'))
                {
                    if (v.Equals(PackageSaveModes.Nupkg.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        EffectivePackageSaveMode |= PackageSaveModes.Nupkg;
                    }
                    else if (v.Equals(PackageSaveModes.Nuspec.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        EffectivePackageSaveMode |= PackageSaveModes.Nuspec;
                    }
                    else
                    {
                        string message = String.Format(
                            CultureInfo.CurrentCulture,
                            LocalizedResourceManager.GetString("Warning_InvalidPackageSaveMode"),
                            v);
                        Console.WriteWarning(message);
                    }
                }
            }
        }

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
                return new AggregateRepository(new[] { CacheRepository, repository });
            }
        }

        protected virtual IPackageManager CreatePackageManager(IFileSystem packagesFolderFileSystem, bool useSideBySidePaths, bool checkDowngrade = true)
        {
            var repository = CreateRepository();
            var pathResolver = new DefaultPackagePathResolver(packagesFolderFileSystem, useSideBySidePaths);
            IPackageRepository localRepository = new LocalPackageRepository(pathResolver, packagesFolderFileSystem);
            if (EffectivePackageSaveMode != PackageSaveModes.None)
            {
                localRepository.PackageSaveMode = EffectivePackageSaveMode;
            }

            var packageManager = new PackageManager(repository, pathResolver, packagesFolderFileSystem, localRepository)
            {
                Logger = Console,
                CheckDowngrade = checkDowngrade
            };

            return packageManager;
        }
    }
}

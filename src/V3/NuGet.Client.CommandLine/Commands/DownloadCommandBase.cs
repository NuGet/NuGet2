using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NuGet.Common;
using NuGet.Client;

namespace NuGet.Commands
{
    public abstract class DownloadCommandBase : Command
    {
        private static readonly IPackageRepository DummySourceRepo = new LocalPackageRepository(@"C:\");
        private readonly IPackageRepository _cacheRepository;
        private readonly List<string> _sources = new List<string>();

        protected PackageSaveModes EffectivePackageSaveMode { get; set; }

        protected DownloadCommandBase(IPackageRepository cacheRepository)
        {
            _cacheRepository = cacheRepository;
        }

        [Option(typeof(NuGetCommandResourceType), "CommandSourceDescription")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option(typeof(NuGetCommandResourceType), "CommandNoCache")]
        public bool NoCache { get; set; }

        [Option(typeof(NuGetCommandResourceType), "CommandDisableParallelProcessing")]
        public bool DisableParallelProcessing { get; set; }

        [Option(typeof(NuGetCommandResourceType), "CommandPackageSaveMode")]
        public string PackageSaveMode { get; set; }
        
        protected void InitializeSourceRepository()
        {
            SourceRepository = SourceRepositoryHelper.CreateSourceRepository(SourceProvider, Source);
        }

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

        // BUGBUG: With AggregateRepositories, this might change. But, less likely to happen
        protected SourceRepository SourceRepository { get; set; }

        /// <remarks>
        /// Meant for unit testing.
        /// </remarks>
        protected IPackageRepository CacheRepository
        {
            get { return _cacheRepository; }
        }

        protected virtual IPackageManager CreatePackageManager(IFileSystem packagesFolderFileSystem, bool useSideBySidePaths, bool checkDowngrade = true)
        {
            var sourceRepository = DummySourceRepo;
            var pathResolver = new DefaultPackagePathResolver(packagesFolderFileSystem, useSideBySidePaths);

            // BUGBUG: Try not to use SharedPackageRepository
            var localRepository = new SharedPackageRepository(
                pathResolver, 
                packagesFolderFileSystem, 
                configSettingsFileSystem: NullFileSystem.Instance);
            if (EffectivePackageSaveMode != PackageSaveModes.None)
            {
                localRepository.PackageSaveMode = EffectivePackageSaveMode;
            }

            var packageManager = new PackageManager(sourceRepository, pathResolver, packagesFolderFileSystem, localRepository)
            {
                Logger = Console,
                CheckDowngrade = checkDowngrade
            };

            return packageManager;
        }
    }

    // The PackageSourceProvider reads from the underlying ISettings multiple times. One of the fields it reads is the password which is consequently decrypted
    // once for each package being installed. Per work item 2345, a couple of users are running into an issue where this results in an exception in native 
    // code. Instead, we'll use a cached set of sources. This should solve the issue and also give us some perf boost.
    public class CachedPackageSourceProvider : IPackageSourceProvider
    {
        private readonly List<PackageSource> _packageSources;

        public CachedPackageSourceProvider(IPackageSourceProvider sourceProvider)
        {
            if (sourceProvider == null)
            {
                throw new ArgumentNullException("sourceProvider");
            }
            _packageSources = sourceProvider.LoadPackageSources().ToList();
        }

        public IEnumerable<PackageSource> LoadPackageSources()
        {
            return _packageSources;
        }

        public void SavePackageSources(IEnumerable<PackageSource> sources)
        {
            PackageSourcesSaved(this, EventArgs.Empty);
            throw new NotSupportedException();
        }

        public void DisablePackageSource(PackageSource source)
        {
            throw new NotSupportedException();
        }

        public bool IsPackageSourceEnabled(PackageSource source)
        {
            return source.IsEnabled;
        }

        public event EventHandler PackageSourcesSaved = delegate { };
    }
}

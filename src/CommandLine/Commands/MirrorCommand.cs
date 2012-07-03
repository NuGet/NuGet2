using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NuGet.Common;

namespace NuGet.Commands
{

    // Thin wrapper that allows exposing a PackageServer as an IPackageRepository
    public class PackageServerRepository : IPackageRepository
    {
        private readonly IPackageRepository _pull;
        private readonly PackageServer _push;
        private readonly string _apiKey;
        private readonly TimeSpan _timeout;
        private readonly ILogger _logger;

        public PackageServerRepository(IPackageRepository pull, PackageServer push, string apiKey, TimeSpan timeout, ILogger logger)
        {
            if (pull == null)
            {
                throw new ArgumentNullException("pull");
            }
            if (push == null)
            {
                throw new ArgumentNullException("push");
            }
            if (apiKey == null)
            {
                throw new ArgumentNullException("apiKey");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            _pull = pull;
            _push = push;
            _apiKey = apiKey;
            _timeout = timeout;
            _logger = logger;
        }

        public string Source
        {
            get { return _pull.Source; }
        }

        public bool SupportsPrereleasePackages
        {
            get { return _pull.SupportsPrereleasePackages; }
        }

        public IQueryable<IPackage> GetPackages()
        {
            return _pull.GetPackages();
        }

        public void AddPackage(IPackage package)
        {
            _logger.Log(MessageLevel.Info, NuGetResources.PushCommandPushingPackage, package.GetFullName(), CommandLineUtility.GetSourceDisplayName(_push.Source));

            using (Stream stream = package.GetStream())
            {
                _push.PushPackage(_apiKey, stream, Convert.ToInt32(_timeout.TotalMilliseconds));
            }

            _logger.Log(MessageLevel.Info, NuGetResources.PushCommandPackagePushed);
        }

        public void RemovePackage(IPackage package)
        {
            throw new NotSupportedException();
        }
    }

    [Command(typeof(NuGetResources), "mirror", "MirrorCommandDescription",
        MinArgs = 3, MaxArgs = 3, UsageDescriptionResourceName = "MirrorCommandUsageDescription",
        UsageSummaryResourceName = "MirrorCommandUsageSummary", UsageExampleResourceName = "MirrorCommandUsageExamples")]
    public class MirrorCommand : Command
    {
        private readonly List<string> _sources = new List<string>();

        [Option(typeof(NuGetResources), "MirrorCommandSourceDescription", AltName="src")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option(typeof(NuGetResources), "MirrorCommandVersionDescription", AltName = "ver")]
        public string Version { get; set; }

        [Option(typeof(NuGetResources), "MirrorCommandApiKey", AltName = "k")]
        public string ApiKey { get; set; }

        [Option(typeof(NuGetResources), "MirrorCommandPrerelease")]
        public bool Prerelease { get; set; }

        [Option(typeof(NuGetResources), "PushCommandTimeoutDescription")]
        public int Timeout { get; set; }

        [Option(typeof(NuGetResources), "MirrorCommandDoCache", AltName = "c")]
        public bool DoCache { get; set; }

        [Option(typeof(NuGetResources), "MirrorCommandNoOp", AltName = "n" )]
        public bool NoOp { get; set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        public ISettings Settings { get; private set; }

        private readonly IPackageRepository _cacheRepository;

        [ImportingConstructor]
        public MirrorCommand(IPackageSourceProvider packageSourceProvider, ISettings settings, IPackageRepositoryFactory packageRepositoryFactory)
        {
            SourceProvider = packageSourceProvider;
            Settings = settings;
            RepositoryFactory = packageRepositoryFactory;
            _cacheRepository = MachineCache.Default;
        }

        protected virtual IPackageRepository CacheRepository
        {
            get { return _cacheRepository; }
        }

        protected virtual IFileSystem CreateFileSystem()
        {
            return new PhysicalFileSystem(Directory.GetCurrentDirectory());
        }

        private IPackageRepository GetSourceRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            bool ignoreFailingRepositories = repository.IgnoreFailingRepositories;
            if (DoCache)
            {
                repository = new AggregateRepository(new[] { CacheRepository, repository }) 
                    { IgnoreFailingRepositories = ignoreFailingRepositories, Logger = Console };
            }
            repository.Logger = Console;
            return repository;
        }

        private IPackageRepository GetDestinationRepositoryList(string repo)
        {
            return RepositoryFactory.CreateRepository(SourceProvider.ResolveAndValidateSource(repo));
        }

        protected virtual IPackageRepository GetTargetRepository(string pull, string push)
        {
            return new PackageServerRepository(
                pull: GetDestinationRepositoryList(pull),
                push: GetDestinationRepositoryPush(push),
                apiKey: GetApiKey(pull),
                timeout: GetTimeout(),
                logger: Console);
        }

        private static PackageServer GetDestinationRepositoryPush(string repo)
        {
            return new PackageServer(repo, CommandLineConstants.UserAgent);
        }

        private PackageMirrorer GetPackageMirrorer(IPackageRepository srcRepository, IPackageRepository dstRepository)
        {
            return new PackageMirrorer(srcRepository, dstRepository)
            {
                Logger = Console,
                NoOp = NoOp
            };
        }

        private string GetApiKey(string source)
        {
            return String.IsNullOrEmpty(ApiKey) ? CommandLineUtility.GetApiKey(Settings, source) : ApiKey;
        }

        private TimeSpan GetTimeout()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(Math.Abs(Timeout));
            return (timeout.Seconds <= 0) ? TimeSpan.FromMinutes(5) : timeout;
        }

        private SemanticVersion GetVersion()
        {
            return null == Version ? null : new SemanticVersion(Version);
        }

        private static PackageReferenceFile GetPackageReferenceFile(IFileSystem fileSystem, string configFilePath)
        {
            // By default the PackageReferenceFile does not throw if the file does not exist at the specified path.
            // We'll try reading from the file so that the file system throws a file not found
            using (fileSystem.OpenFile(configFilePath))
            {
                // Do nothing
            }
            return new PackageReferenceFile(fileSystem, Path.GetFullPath(configFilePath));
        }

        private static bool IsUsingPackagesConfig(string packageId)
        {
            return Path.GetFileName(packageId).Equals(Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<Tuple<string, SemanticVersion>> GetPackagesToMirror(string packageId, bool isPackagesConfig)
        {
            if (isPackagesConfig)
            {
                IFileSystem fileSystem = CreateFileSystem();
                string configFilePath = Path.GetFullPath(packageId);
                var packageReferenceFile = GetPackageReferenceFile(fileSystem, configFilePath);
                var packageReferences = CommandLineUtility.GetPackageReferences(packageReferenceFile, configFilePath, requireVersion: false);
                return
                    from pr in packageReferences
                    select new Tuple<string, SemanticVersion>(pr.Id, pr.Version);
            }
            else
            {
                return new[] { new Tuple<string, SemanticVersion>(packageId, GetVersion()) };
            }
        }

        bool AllowPrereleaseVersion(SemanticVersion version, bool isUsingPackagesConfig)
        {
            if (isUsingPackagesConfig && (null != version))
            {
                return true;
            }
            return Prerelease;            
        }

        public override void ExecuteCommand()
        {
            var srcRepository = GetSourceRepository();
            var dstRepository = GetTargetRepository(Arguments[1], Arguments[2]);
            var mirrorer = GetPackageMirrorer(srcRepository, dstRepository);
            var isPackagesConfig = IsUsingPackagesConfig(Arguments[0]);
            var toMirror = GetPackagesToMirror(Arguments[0], isPackagesConfig);            

            if (isPackagesConfig && (null != Version))
            {
                throw new ArgumentException(NuGetResources.MirrorCommandNoVersionIfPackagesConfig);
            }

            bool didSomething = false;

            using (mirrorer.SourceRepository.StartOperation(RepositoryOperationNames.Mirror))
            {
                foreach (var package in toMirror)
                {
                    if (mirrorer.MirrorPackage(
                        packageId: package.Item1,
                        version: package.Item2,
                        ignoreDependencies: false,
                        allowPrereleaseVersions: AllowPrereleaseVersion(package.Item2, isPackagesConfig)))
                        didSomething = true;
                }
            }

            if (! didSomething)
            {
                Console.Log(MessageLevel.Warning, NuGetResources.MirrorCommandDidNothing);
            }
        }

    }
}
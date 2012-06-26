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

        private IPackageRepository GetSourceRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            bool ignoreFailingRepositories = repository.IgnoreFailingRepositories;
            if (DoCache)
            {
                repository = new AggregateRepository(new[] { _cacheRepository, repository }) 
                    { IgnoreFailingRepositories = ignoreFailingRepositories, Logger = Console };
            }
            repository.Logger = Console;
            return repository;
        }

        private IPackageRepository GetDestinationRepositoryList(string repo)
        {
            repo = SourceProvider.ResolveAndValidateSource(repo);
            return RepositoryFactory.CreateRepository(repo);
        }

        private IPackageRepository GetTargetRepository(string pullUrl, string pushUrl)
        {
            return new PackageServerRepository(
                pull: GetDestinationRepositoryList(pullUrl),
                push: GetDestinationRepositoryPush(pushUrl),
                apiKey: GetApiKey(pullUrl),
                timeout: GetTimeout(),
                logger: Console);
        }

        private PackageServer GetDestinationRepositoryPush(string repo)
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
            return null == Version? null : new SemanticVersion(Version);
        }

        public override void ExecuteCommand()
        {
            string packageId = Arguments[0];
            if (Path.GetFileName(packageId).Equals(Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException("Comming soon.");
            }
            
            SemanticVersion version = GetVersion();
            IPackageRepository srcRepository = GetSourceRepository();
            IPackageRepository dstRepository = GetTargetRepository(Arguments[1], Arguments[2]);
            PackageMirrorer mirrorer = GetPackageMirrorer(srcRepository, dstRepository);

            using (mirrorer.SourceRepository.StartOperation(RepositoryOperationNames.Mirror))
            {
                mirrorer.MirrorPackage(packageId, version, ignoreDependencies: false, allowPrereleaseVersions: Prerelease);
            }
        }

    }
}
using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Common;
using NuGet.Commands;

namespace NuGet.ServerExtensions
{
    [Command(typeof(NuGetResources), "mirror", "MirrorCommandDescription",
        MinArgs = 3, MaxArgs = 3, UsageDescriptionResourceName = "MirrorCommandUsageDescription",
        UsageSummaryResourceName = "MirrorCommandUsageSummary", UsageExampleResourceName = "MirrorCommandUsageExamples")]
    public class MirrorCommand : DownloadCommandBase
    {
        [Option(typeof(NuGetResources), "MirrorCommandVersionDescription")]
        public string Version { get; set; }

        [Option(typeof(NuGetResources), "MirrorCommandApiKey", AltName = "k")]
        public string ApiKey { get; set; }

        [Option(typeof(NuGetResources), "MirrorCommandPrerelease")]
        public bool Prerelease { get; set; }

        [Option(typeof(NuGetResources), "MirrorCommandTimeoutDescription")]
        public int Timeout { get; set; }

        [Option(typeof(NuGetResources), "MirrorCommandNoOp", AltName = "n")]
        public bool NoOp { get; set; }

        [Option(typeof(NuGetResources), "MirrorCommandDependenciesMode")]
        public MirrorDependenciesMode DependenciesMode { get; set; }

        public MirrorCommand() : base(MachineCache.Default)
        {
        }

        // for unit test
        protected MirrorCommand(IPackageRepository cacheRepository) : 
            base(cacheRepository)
        {
        }

        public override void ExecuteCommand()
        {
            var srcRepository = CreateRepository();
            var dstRepository = GetTargetRepository(Arguments[1], Arguments[2]);
            var mirrorer = GetPackageMirrorer(srcRepository, dstRepository);
            var isConfigFile = IsConfigFile(Arguments[0]);
            var toMirror = GetPackagesToMirror(Arguments[0], isConfigFile);

            if (isConfigFile && !String.IsNullOrEmpty(Version))
            {
                throw new ArgumentException(NuGetResources.MirrorCommandNoVersionIfUsingConfigFile);
            }

            int countMirrored=0;

            using (mirrorer.SourceRepository.StartOperation(RepositoryOperationNames.Mirror, mainPackageId: null, mainPackageVersion: null))
            {
                foreach (var package in toMirror)
                {
                    countMirrored += mirrorer.MirrorPackage(
                        package.Id,
                        package.Version,
                        AllowPrereleaseVersion(package.Version, isConfigFile),
                        DependenciesMode);
                }
            }

            Console.Log(MessageLevel.Info, NuGetResources.MirrorCommandCountMirrored, countMirrored);
        }

        protected virtual IFileSystem CreateFileSystem()
        {
            var physicalFileSystem = new PhysicalFileSystem(Directory.GetCurrentDirectory());
            physicalFileSystem.Logger = Console;
            return physicalFileSystem;
        }

        private IPackageRepository GetDestinationRepositoryList(string repo)
        {
            return RepositoryFactory.CreateRepository(SourceProvider.ResolveAndValidateSource(repo));
        }

        protected virtual IPackageRepository GetTargetRepository(string pull, string push)
        {
            return new PackageServerRepository(
                sourceRepository: GetDestinationRepositoryList(pull),
                destination: GetDestinationRepositoryPush(push),
                apiKey: GetApiKey(pull),
                timeout: GetTimeout(),
                logger: Console);
        }

        private static PackageServer GetDestinationRepositoryPush(string repo)
        {
            return new PackageServer(repo, userAgent: "NuGet Command Line");
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

        private static bool IsConfigFile(string packageId)
        {
            return
                // support of packages.config is for backwards compatibility.
                Path.GetFileName(packageId).Equals(Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(packageId).Equals(Constants.MirroringReferenceFile, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<PackageReference> GetPackagesToMirror(string packageId, bool isConfigFile)
        {
            if (isConfigFile)
            {
                IFileSystem fileSystem = CreateFileSystem();
                string configFilePath = Path.GetFullPath(packageId);
                var packageReferenceFile = GetPackageReferenceFile(fileSystem, configFilePath);
                return CommandLineUtility.GetPackageReferences(packageReferenceFile, requireVersion: false);
            }
            
            return new[] { new PackageReference(packageId, GetVersion(), versionConstraint: null, targetFramework: null, isDevelopmentDependency: false) };
        }

        private bool AllowPrereleaseVersion(SemanticVersion version, bool isUsingPackagesConfig)
        {
            if (isUsingPackagesConfig && (null != version))
            {
                return true;
            }
            return Prerelease;
        }
    }
}
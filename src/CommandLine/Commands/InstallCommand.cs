using System;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "install", "InstallCommandDescription", 
        MinArgs = 1, MaxArgs = 1,
        UsageSummaryResourceName = "InstallCommandUsageSummary", UsageDescriptionResourceName = "InstallCommandUsageDescription")]
    public class InstallCommand : Command {
        private const string DefaultFeedUrl = ListCommand.DefaultFeedUrl;

        [Option(typeof(NuGetResources), "InstallCommandSourceDescription")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "InstallCommandVersionDescription")]
        public string Version { get; set; }

        [Option(typeof(NuGetResources), "InstallCommandExcludeVersionDescription")]
        public bool ExcludeVersion { get; set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        [ImportingConstructor]
        public InstallCommand(IPackageRepositoryFactory packageRepositoryFactory) {
            if (packageRepositoryFactory == null) {
                throw new ArgumentNullException("packageRepositoryFactory");
            }

            RepositoryFactory = packageRepositoryFactory;
        }

        public override void ExecuteCommand() {
            var feedUrl = DefaultFeedUrl;
            if (!String.IsNullOrEmpty(Source)) {
                feedUrl = Source;
            }

            IPackageRepository packageRepository = RepositoryFactory.CreateRepository(new PackageSource(feedUrl));
            string path = Directory.GetCurrentDirectory();
            var packageManager = new PackageManager(packageRepository,
                                                    new DefaultPackagePathResolver(path, useSideBySidePaths: !ExcludeVersion), 
                                                    new PhysicalFileSystem(path));

            packageManager.Logger = new Logger { Console = Console };

            string packageId = Arguments[0];
            Version version = Version != null ? new Version(Version) : null;
            packageManager.InstallPackage(packageId, version);
        }

        private class Logger : ILogger {
            public IConsole Console { get; set; }

            public void Log(MessageLevel level, string message, params object[] args) {
                Console.WriteLine(message, args);
            }
        }
    }
}

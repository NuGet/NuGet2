using System;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "install", "InstallCommandDescription", AltName = "i",
        MinArgs = 1, MaxArgs = 1,
        UsageSummaryResourceName = "InstallCommandUsageSummary", UsageDescriptionResourceName = "InstallCommandUsageDescription")]
    public class InstallCommand : Command {
        private const string _defaultFeedUrl = ListCommand._defaultFeedUrl;

        [Option(typeof(NuGetResources), "InstallCommandSourceDescription", AltName = "s")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "InstallCommandVersionDescription", AltName = "v")]
        public string Version { get; set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        [ImportingConstructor]
        public InstallCommand(IPackageRepositoryFactory packageRepositoryFactory) {
            if (packageRepositoryFactory == null) {
                throw new ArgumentNullException("packageRepositoryFactory");
            }

            RepositoryFactory = packageRepositoryFactory;
        }

        public override void ExecuteCommand() {
            var feedUrl = _defaultFeedUrl;
            if (!String.IsNullOrEmpty(Source)) {
                feedUrl = Source;
            }

            IPackageRepository packageRepository = RepositoryFactory.CreateRepository(new PackageSource(feedUrl));
            var packageManager = new PackageManager(packageRepository, Directory.GetCurrentDirectory());

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

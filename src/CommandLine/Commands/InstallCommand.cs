using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "install", "InstallCommandDescription", AltName = "i",
        MinArgs = 1, MaxArgs = 1,
        UsageSummaryResourceName = "InstallCommandUsageSummary", UsageDescriptionResourceName = "InstallCommandUsageDescription")]
    public class InstallCommand : ICommand {
        private const string _defaultFeedUrl = ListCommand._defaultFeedUrl;

        [Option(typeof(NuGetResources), "InstallCommandSourceDescription", AltName = "s")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "InstallCommandVersionDescription", AltName = "v")]
        public string Version { get; set; }

        public List<string> Arguments { get; set; }

        public IConsole Console { get; private set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        [ImportingConstructor]
        public InstallCommand(IPackageRepositoryFactory packageRepositoryFactory, IConsole console) {
            if (console == null) {
                throw new ArgumentNullException("console");
            }

            if (packageRepositoryFactory == null) {
                throw new ArgumentNullException("packageRepositoryFactory");
            }

            Console = console;
            RepositoryFactory = packageRepositoryFactory;
        }

        public void Execute() {
            var feedUrl = _defaultFeedUrl;
            if (!String.IsNullOrEmpty(Source)) {
                feedUrl = Source;
            }

            IPackageRepository packageRepository = RepositoryFactory.CreateRepository(new PackageSource(feedUrl));
            var packageManager = new PackageManager(packageRepository, "packages");

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

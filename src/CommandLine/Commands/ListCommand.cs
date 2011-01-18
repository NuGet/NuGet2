using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "list", "ListCommandDescription", AltName = "l",
        UsageSummaryResourceName = "ListCommandUsageSummary", UsageDescriptionResourceName = "ListCommandUsageDescription")]
    public class ListCommand : Command {
        internal const string _defaultFeedUrl = "https://go.microsoft.com/fwlink/?LinkID=206669";

        [Option(typeof(NuGetResources), "ListCommandSourceDescription", AltName = "s")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "ListCommandVerboseListDescription", AltName = "v")]
        public bool Verbose { get; set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        [ImportingConstructor]
        public ListCommand(IPackageRepositoryFactory packageRepositoryFactory) {
            if (packageRepositoryFactory == null) {
                throw new ArgumentNullException("packageRepositoryFactory");
            }

            RepositoryFactory = packageRepositoryFactory;
        }

        public IQueryable<IPackage> GetPackages() {
            var feedUrl = _defaultFeedUrl;
            if (!String.IsNullOrEmpty(Source)) {
                feedUrl = Source;
            }

            var packageRepository = RepositoryFactory.CreateRepository(new PackageSource(feedUrl, "feed"));

            if (Arguments != null && Arguments.Any()) {
                return packageRepository.GetPackages().Find(Arguments.ToArray());
            }

            return packageRepository.GetPackages();
        }

        public override void ExecuteCommand() {

            IEnumerable<IPackage> packages = GetPackages();

            bool hasPackages = false;

            if (packages != null) {
                if (Verbose) {
                    /***********************************************
                     * Package-Name
                     *  1.0.0.2010
                     *  This is the package Description
                     * 
                     * Package-Name-Two
                     *  2.0.0.2010
                     *  This is the second package Description
                     ***********************************************/
                    foreach (var p in packages) {
                        Console.PrintJustified(0, p.Id);
                        Console.PrintJustified(1, p.Version.ToString());
                        Console.PrintJustified(1, p.Description);
                        Console.WriteLine();
                        hasPackages = true;
                    }
                }
                else {
                    /***********************************************
                     * Package-Name 1.0.0.2010
                     * Package-Name-Two 2.0.0.2010
                     ***********************************************/
                    foreach (var p in packages) {
                        Console.PrintJustified(0, p.GetFullName());
                        hasPackages = true;
                    }
                }
            }

            if (!hasPackages) {
                Console.WriteLine(NuGetResources.ListCommandNoPackages);
            }
        }
    }
}
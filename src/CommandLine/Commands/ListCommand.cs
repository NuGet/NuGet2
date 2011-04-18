using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "list", "ListCommandDescription", 
        UsageSummaryResourceName = "ListCommandUsageSummary", UsageDescriptionResourceName = "ListCommandUsageDescription")]
    public class ListCommand : Command {
        internal const string DefaultFeedUrl = "https://go.microsoft.com/fwlink/?LinkID=206669";

        [Option(typeof(NuGetResources), "ListCommandSourceDescription")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "ListCommandVerboseListDescription")]
        public bool Verbose { get; set; }

        [Option(typeof(NuGetResources), "ListCommandAllVersionsDescription")]
        public bool AllVersions { get; set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        [ImportingConstructor]
        public ListCommand(IPackageRepositoryFactory packageRepositoryFactory) {
            if (packageRepositoryFactory == null) {
                throw new ArgumentNullException("packageRepositoryFactory");
            }

            RepositoryFactory = packageRepositoryFactory;
        }

        public IEnumerable<IPackage> GetPackages() {
            var feedUrl = DefaultFeedUrl;
            if (!String.IsNullOrEmpty(Source)) {
                feedUrl = Source;
            }

            var packageRepository = RepositoryFactory.CreateRepository(new PackageSource(feedUrl, "feed"));
            
            IQueryable<IPackage> packages = packageRepository.GetPackages();
            if (Arguments != null && Arguments.Any()) {
                packages = packages.Find(Arguments.ToArray());
            }
            if (AllVersions) {
                // Do not collapse versions
                return packages;
            }
            return packages.DistinctLast(PackageEqualityComparer.Id, PackageComparer.Version);
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
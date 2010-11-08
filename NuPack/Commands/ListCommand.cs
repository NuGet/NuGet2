namespace NuGet.Commands {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using NuGet.Common;

    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "list", "ListCommandDescription", AltName = "l",
        UsageSummaryResourceName = "ListCommandUsageSummary", UsageDescriptionResourceName = "ListCommandUsageDescription")]
    public class ListCommand : ICommand {
        private const string _defaultFeedUrl = "http://go.microsoft.com/fwlink/?LinkID=204820";

        [Option(typeof(NuGetResources), "ListCommandSourceDescription", AltName = "s")]
        public string Source { get; set; }
        [Option(typeof(NuGetResources), "ListCommandVerboseListDescription", AltName = "v")]
        public bool Verbose { get; set; }
        public List<string> Arguments { get; set; }
        [Import(typeof(IConsole))]
        public IConsole Console { get; set; }
        public IPackageRepositoryFactory packageRepositoryFactory { get; set; }

        public ListCommand() : this(PackageRepositoryFactory.Default) { }

        public ListCommand(IPackageRepositoryFactory packageFactory) {
            packageRepositoryFactory = packageFactory;
        }

        public IQueryable<IPackage> GetPackages() {
            var feedUrl = _defaultFeedUrl;
            if (!String.IsNullOrEmpty(Source)) {
                feedUrl = Source;
            }

            var packageRepository = packageRepositoryFactory.CreateRepository(feedUrl);

            if (Arguments != null && Arguments.Any()) {
                return packageRepository.GetPackages(Arguments.ToArray());
            }

            return packageRepository.GetPackages();
        }

        public void Execute() {

            IEnumerable<IPackage> packages = GetPackages();

            if (packages != null && packages.Any()) {
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
                    }
                }
                else {
                    /***********************************************
                     * Package-Name 1.0.0.2010
                     * Package-Name-Two 2.0.0.2010
                     ***********************************************/
                    foreach (var p in packages) {
                        Console.PrintJustified(0, p.GetFullName());
                    }
                }
            }
            else {
                Console.WriteLine(NuGetResources.ListCommandNoPackages);
            }
        }
    }
}

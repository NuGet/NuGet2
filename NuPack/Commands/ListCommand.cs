namespace NuGet.Commands {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using NuGet.Common;

    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "list", "ListCommandDescription", MaxArgs = 1, AltName = "l",
        UsageSummaryResourceName = "ListCommandUsageSummary", UsageDescriptionResourceName = "ListCommandUsageDescription")]
    public class ListCommand : ICommand {
        private const string _defaultFeedUrl = "http://go.microsoft.com/fwlink/?LinkID=204820";


        [Option(typeof(NuGetResources), "ListCommandSourceDescription", AltName = "s")]
        public string Source { get; set; }
        [Option(typeof(NuGetResources), "ListCommandDetailedListDescription", AltName = "dl")]
        public bool DetailedList { get; set; }
        public List<string> Arguments { get; set; }
        [Import(typeof(IConsoleWriter))]
        public IConsoleWriter Console { get; set; }

        public IQueryable<IPackage> GetPackages() {
            var feedUrl = _defaultFeedUrl;
            if (!String.IsNullOrEmpty(Source)) {
                feedUrl = Source;
            }

            var packageRepository = PackageRepositoryFactory.Default.CreateRepository(feedUrl);

            if (Arguments.Count > 0) {
                return packageRepository.GetPackages(Arguments.ToArray());
            }

            return packageRepository.GetPackages();
        }

        public void Execute() {

            var packages = GetPackages();

            if (packages != null && packages.Count() > 0) {
                if (DetailedList) {
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
                Console.WriteLine("No packages found.");
            }
        }
    }
}

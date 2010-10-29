namespace NuGet.Commands {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using NuGet.Common;

    [Export(typeof(ICommand))]
    [Command("List", "List the packages in a feed", AltName = "l")]
    public class ListCommand : ICommand {
        private const string _defaultFeedUrl = "http://go.microsoft.com/fwlink/?LinkID=204820";


        [Option("Feed Source", AltName = "s")]
        public string Source { get; set; }
        public List<string> Arguments { get; set; }

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
            ConsoleWriter console = new ConsoleWriter();

            console.WriteLine("Packages in feed:");
            foreach (var p in GetPackages()) {
                console.WriteLine("  {0}", p.GetFullName());

            }
        }
    }
}

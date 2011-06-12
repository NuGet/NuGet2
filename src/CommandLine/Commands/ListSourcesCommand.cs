using System;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "listsources", "ListSourcesCommandDescription")]
    public class ListSourcesCommand : Command {

        public IPackageSourceProvider SourceProvider { get; private set; }

        [ImportingConstructor]
        public ListSourcesCommand(IPackageSourceProvider sourceProvider) {
            if (sourceProvider == null) {
                throw new ArgumentNullException("sourceProvider");
            }
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand() {
            var sourcesList = SourceProvider.LoadPackageSources().ToList();
            if(!sourcesList.Any()) {
                Console.WriteLine(NuGetResources.ListSourcesCommandNoSources);
                return;
            }
            Console.PrintJustified(0, NuGetResources.ListSourcesCommandRegisteredSources);
            Console.WriteLine();
            var sourcePadding = Enumerable.Repeat(" ", 6);
            for (int i = 0; i < sourcesList.Count; i++) {
                var source = sourcesList[i];
                var indexNumber = i + 1;
                var namePadding = i >= 9 ? Enumerable.Repeat(" ", 1) : Enumerable.Repeat(" ", 2);
                Console.WriteLine("  {0}.{1}{2}", indexNumber, string.Concat(namePadding), source.Name);
                Console.WriteLine("{0}{1}", string.Concat(sourcePadding), source.Source);
            }
        }
    }
}

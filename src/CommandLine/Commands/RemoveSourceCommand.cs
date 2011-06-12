using System;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "removesource", "RemoveSourceCommandDescription", UsageSummaryResourceName = "RemoveSourceCommandUsageSummary")]
    public class RemoveSourceCommand : Command {

        [Option(typeof(NuGetResources), "NameDescription")]
        public string Name { get;set; }

        public IPackageSourceProvider SourceProvider { get;private set; }

        [ImportingConstructor]
        public RemoveSourceCommand(IPackageSourceProvider sourceProvider) {
            if (sourceProvider == null) {
                throw new ArgumentNullException("sourceProvider");
            }
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand() {
            if (String.IsNullOrWhiteSpace(Name)) {
                Console.WriteError(NuGetResources.NameRequired);
                return;
            }
            // Check to see if we already have a registered source with the same name or source
            var sourceList = SourceProvider.LoadPackageSources().ToList();
            var existingSource = sourceList.Where(ps => String.Equals(Name, ps.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!existingSource.Any()) {
                Console.WriteError(NuGetResources.RemoveSourceCommandNoMatchingSourcesFound, Name);
                return;
            }

            existingSource.ForEach(source => sourceList.Remove(source));
            SourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(NuGetResources.RemoveSourceCommandSourceAddedSuccessfully, Name);
        }
    }
}

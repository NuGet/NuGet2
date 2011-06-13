using System;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "removesource", "RemoveSourceCommandDescription", UsageSummaryResourceName = "RemoveSourceCommandUsageSummary",
        MinArgs = 1,MaxArgs = 1)]
    public class RemoveSourceCommand : Command {

        public IPackageSourceProvider SourceProvider { get;private set; }

        [ImportingConstructor]
        public RemoveSourceCommand(IPackageSourceProvider sourceProvider) {
            if (sourceProvider == null) {
                throw new ArgumentNullException("sourceProvider");
            }
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand() {
            string sourceName = Arguments[0];
            if (String.IsNullOrWhiteSpace(sourceName)) {
                throw new CommandLineException(NuGetResources.NameRequired);
            }
            // Check to see if we already have a registered source with the same name or source
            var sourceList = SourceProvider.LoadPackageSources().ToList();
            var existingSource = sourceList.Where(ps => String.Equals(sourceName, ps.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!existingSource.Any()) {
                throw new CommandLineException(NuGetResources.RemoveSourceCommandNoMatchingSourcesFound, sourceName);
            }

            existingSource.ForEach(source => sourceList.Remove(source));
            SourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(NuGetResources.RemoveSourceCommandSourceAddedSuccessfully, sourceName);
        }
    }
}

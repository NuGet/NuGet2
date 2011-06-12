using System;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "addsource", "AddSourceCommandDescription", UsageSummaryResourceName = "AddSourceCommandUsageSummary",
        MinArgs = 2,MaxArgs = 2)]
    public class AddSourceCommand: Command {

        public IPackageSourceProvider SourceProvider { get; private set; }

        [ImportingConstructor]
        public AddSourceCommand(IPackageSourceProvider sourceProvider) {
            if (sourceProvider == null) {
                throw new ArgumentNullException("sourceProvider");
            }
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand() {
            string sourceName = Arguments[0];
            if (String.IsNullOrWhiteSpace(sourceName)) {
                Console.WriteError(NuGetResources.NameRequired);
                return;
            }
            if (String.Equals(sourceName, NuGetResources.ReservedPackageNameAll)) {
                Console.WriteError(NuGetResources.AddSourceCommandAllNameIsReserved);
                return;
            }
            string source = Arguments[1];
            if (String.IsNullOrWhiteSpace(source)) {
                Console.WriteError(NuGetResources.AddSourceCommandSourceRequired);
                return;
            }

            // Make sure that the Source given is a valid one.
            if (!PathValidator.IsValidSource(source)) {
                Console.WriteError(NuGetResources.AddSourceCommandInvalidSource);
                return;
            }
            // Check to see if we already have a registered source with the same name or source
            var sourceList = SourceProvider.LoadPackageSources().ToList();
            bool hasName = sourceList.Any(ps => String.Equals(sourceName, ps.Name, StringComparison.OrdinalIgnoreCase));
            if (hasName) {
                Console.WriteError(NuGetResources.AddSourceCommandUniqueName);
                return;
            }
            bool hasSource = sourceList.Any(ps => String.Equals(source, ps.Source, StringComparison.OrdinalIgnoreCase));
            if (hasSource) {
                Console.WriteError(NuGetResources.AddSourceCommandUniqueSource);
                return;                
            }

            var newPackageSource = new PackageSource(source, sourceName);
            sourceList.Add(newPackageSource);
            SourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(NuGetResources.AddSourceCommandSourceAddedSuccessfully, sourceName);
        }
    }
}

using System;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "addsource", "AddSourceCommandDescription", UsageSummaryResourceName = "AddSourceCommandUsageSummary")]
    public class AddSourceCommand: Command {

        [Option(typeof(NuGetResources),"AddSourceCommandNameDescription")]
        public string Name { get; set; }

        [Option(typeof(NuGetResources), "AddSourceCommandSourceDescription", AltName = "src")]
        public string Source { get;set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        [ImportingConstructor]
        public AddSourceCommand(IPackageSourceProvider sourceProvider) {
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
            if (String.IsNullOrWhiteSpace(Source)) {
                Console.WriteError(NuGetResources.AddSourceCommandSourceRequired);
                return;
            }
            // Make sure that the Source given is a valid one.
            if (!(PathValidator.IsValidLocalPath(Source) || PathValidator.IsValidUncPath(Source) || PathValidator.IsValidUrl(Source))) {
                Console.WriteError(NuGetResources.AddSourceCommandInvalidSource);
                return;
            }
            // Check to see if we already have a registered source with the same name or source
            var sourceList = SourceProvider.LoadPackageSources().ToList();
            bool hasName = sourceList.Any(ps => String.Equals(Name, ps.Name, StringComparison.OrdinalIgnoreCase));
            if (hasName) {
                Console.WriteError(NuGetResources.AddSourceCommandUniqueName);
                return;
            }
            bool hasSource = sourceList.Any(ps => String.Equals(Source, ps.Source, StringComparison.OrdinalIgnoreCase));
            if (hasSource) {
                Console.WriteError(NuGetResources.AddSourceCommandUniqueSource);
                return;                
            }

            var newPackageSource = new PackageSource(Source, Name);
            sourceList.Add(newPackageSource);
            SourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(NuGetResources.AddSourceCommandSourceAddedSuccessfully, Name);
        }
    }
}

using System;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "sources", "SourcesCommandDescription", UsageSummaryResourceName = "SourcesCommandUsageSummary",
        MinArgs = 0, MaxArgs = 1)]
    public class SourcesCommand : Command {

        [Option(typeof(NuGetResources), "SourcesCommandNameDescription")]
        public string Name { get; set; }

        [Option(typeof(NuGetResources), "SourcesCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        [ImportingConstructor]
        public SourcesCommand(IPackageSourceProvider sourceProvider) {
            if (sourceProvider == null) {
                throw new ArgumentNullException("sourceProvider");
            }
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand() {
            // Convert to update
            var action = Arguments.Any() ? Arguments.First().ToUpperInvariant() : null;
            switch (action) {
                case null:
                case "LIST":
                    PrintRegisteredSources();
                    break;
                case "ADD":
                    AddNewSource(Name, Source);
                    break;
                case "REMOVE":
                    RemoveSource(Name);
                    break;
            }
        }

        private void RemoveSource(string name) {
            if (String.IsNullOrWhiteSpace(name)) {
                throw new CommandLineException(NuGetResources.SourcesCommandNameRequired);
            }
            // Check to see if we already have a registered source with the same name or source
            var sourceList = SourceProvider.LoadPackageSources().ToList();
            var existingSource = sourceList.Where(ps => String.Equals(name, ps.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!existingSource.Any()) {
                throw new CommandLineException(NuGetResources.SourcesCommandNoMatchingSourcesFound, name);
            }

            existingSource.ForEach(source => sourceList.Remove(source));
            SourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(NuGetResources.SourcesCommandSourceRemovedSuccessfully, name);
        }

        private void AddNewSource(string name, string source) {
            if (String.IsNullOrWhiteSpace(name)) {
                throw new CommandLineException(NuGetResources.SourcesCommandNameRequired);
            }
            if (String.Equals(name, NuGetResources.ReservedPackageNameAll)) {
                throw new CommandLineException(NuGetResources.SourcesCommandAllNameIsReserved);
            }
            if (String.IsNullOrWhiteSpace(source)) {
                throw new CommandLineException(NuGetResources.SourcesCommandSourceRequired);
            }
            // Make sure that the Source given is a valid one.
            if (!PathValidator.IsValidSource(source)) {
                throw new CommandLineException(NuGetResources.SourcesCommandInvalidSource);
            }
            // Check to see if we already have a registered source with the same name or source
            var sourceList = SourceProvider.LoadPackageSources().ToList();
            bool hasName = sourceList.Any(ps => String.Equals(name, ps.Name, StringComparison.OrdinalIgnoreCase));
            if (hasName) {
                throw new CommandLineException(NuGetResources.SourcesCommandUniqueName);
            }
            bool hasSource = sourceList.Any(ps => String.Equals(source, ps.Source, StringComparison.OrdinalIgnoreCase));
            if (hasSource) {
                throw new CommandLineException(NuGetResources.SourcesCommandUniqueSource);
            }

            var newPackageSource = new PackageSource(source, name);
            sourceList.Add(newPackageSource);
            SourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(NuGetResources.SourcesCommandSourceAddedSuccessfully, name);
        }

        private void PrintRegisteredSources() {
            var sourcesList = SourceProvider.LoadPackageSources().ToList();
            if (!sourcesList.Any()) {
                Console.WriteLine(NuGetResources.SourcesCommandNoSources);
                return;
            }
            Console.PrintJustified(0, NuGetResources.SourcesCommandRegisteredSources);
            Console.WriteLine();
            var sourcePadding = new String(' ', 6);
            for (int i = 0; i < sourcesList.Count; i++) {
                var source = sourcesList[i];
                var indexNumber = i + 1;
                var namePadding = new String(' ', i >= 9 ? 1 : 2);
                Console.WriteLine("  {0}.{1}{2}", indexNumber, namePadding, source.Name);
                Console.WriteLine("{0}{1}", sourcePadding, source.Source);
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommand), "sources", "SourcesCommandDescription", UsageSummaryResourceName = "SourcesCommandUsageSummary",
        MinArgs = 0, MaxArgs = 1)]
    public class SourcesCommand : Command
    {
        private readonly IPackageSourceProvider _sourceProvider;

        [Option(typeof(NuGetCommand), "SourcesCommandNameDescription")]
        public string Name { get; set; }

        [Option(typeof(NuGetCommand), "SourcesCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        [Option(typeof(NuGetCommand), "SourcesCommandUserNameDescription")]
        public string UserName { get; set; }

        [Option(typeof(NuGetCommand), "SourcesCommandPasswordDescription")]
        public string Password { get; set; }

        [ImportingConstructor]
        public SourcesCommand(IPackageSourceProvider sourceProvider)
        {
            if (sourceProvider == null)
            {
                throw new ArgumentNullException("sourceProvider");
            }
            _sourceProvider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            // Convert to update
            var action = Arguments.FirstOrDefault();

            // TODO: Change these in to switches so we don't have to parse them here.
            if (String.IsNullOrEmpty(action) || action.Equals("List", StringComparison.OrdinalIgnoreCase))
            {
                PrintRegisteredSources();
            }
            else if (action.Equals("Add", StringComparison.OrdinalIgnoreCase))
            {
                AddNewSource();
            }
            else if (action.Equals("Remove", StringComparison.OrdinalIgnoreCase))
            {
                RemoveSource();
            }
            else if (action.Equals("Enable", StringComparison.OrdinalIgnoreCase))
            {
                EnableOrDisableSource(enabled: true);
            }
            else if (action.Equals("Disable", StringComparison.OrdinalIgnoreCase))
            {
                EnableOrDisableSource(enabled: false);
            }
            else if (action.Equals("Update", StringComparison.OrdinalIgnoreCase))
            {
                UpdatePackageSource();
            }
        }

        private void EnableOrDisableSource(bool enabled)
        {
            if (String.IsNullOrEmpty(Name))
            {
                throw new CommandLineException(NuGetResources.SourcesCommandNameRequired);
            }

            List<PackageSource> sourceList = _sourceProvider.LoadPackageSources().ToList();
            var existingSource = sourceList.Where(ps => String.Equals(Name, ps.Name, StringComparison.OrdinalIgnoreCase));
            if (!existingSource.Any())
            {
                throw new CommandLineException(NuGetResources.SourcesCommandNoMatchingSourcesFound, Name);
            }

            foreach (var source in existingSource)
            {
                source.IsEnabled = enabled;
            }

            _sourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(
                enabled ? NuGetResources.SourcesCommandSourceEnabledSuccessfully : NuGetResources.SourcesCommandSourceDisabledSuccessfully,
                Name);
        }

        private void RemoveSource()
        {
            if (String.IsNullOrEmpty(Name))
            {
                throw new CommandLineException(NuGetResources.SourcesCommandNameRequired);
            }
            // Check to see if we already have a registered source with the same name or source
            var sourceList = _sourceProvider.LoadPackageSources().ToList();
            var matchingSources = sourceList.Where(ps => String.Equals(Name, ps.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!matchingSources.Any())
            {
                throw new CommandLineException(NuGetResources.SourcesCommandNoMatchingSourcesFound, Name);
            }

            sourceList.RemoveAll(matchingSources.Contains);
            _sourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(NuGetResources.SourcesCommandSourceRemovedSuccessfully, Name);
        }

        private void AddNewSource()
        {
            if (String.IsNullOrEmpty(Name))
            {
                throw new CommandLineException(NuGetResources.SourcesCommandNameRequired);
            }
            if (String.Equals(Name, NuGetResources.ReservedPackageNameAll))
            {
                throw new CommandLineException(NuGetResources.SourcesCommandAllNameIsReserved);
            }
            if (String.IsNullOrEmpty(Source))
            {
                throw new CommandLineException(NuGetResources.SourcesCommandSourceRequired);
            }
            // Make sure that the Source given is a valid one.
            if (!PathValidator.IsValidSource(Source))
            {
                throw new CommandLineException(NuGetResources.SourcesCommandInvalidSource);
            }

            ValidateCredentials();

            // Check to see if we already have a registered source with the same name or source
            var sourceList = _sourceProvider.LoadPackageSources().ToList();
            bool hasName = sourceList.Any(ps => String.Equals(Name, ps.Name, StringComparison.OrdinalIgnoreCase));
            if (hasName)
            {
                throw new CommandLineException(NuGetResources.SourcesCommandUniqueName);
            }
            bool hasSource = sourceList.Any(ps => String.Equals(Source, ps.Source, StringComparison.OrdinalIgnoreCase));
            if (hasSource)
            {
                throw new CommandLineException(NuGetResources.SourcesCommandUniqueSource);
            }

            var newPackageSource = new PackageSource(Source, Name) { UserName = UserName, Password = Password };
            sourceList.Add(newPackageSource);
            _sourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(NuGetResources.SourcesCommandSourceAddedSuccessfully, Name);
        }

        private void UpdatePackageSource()
        {
            if (String.IsNullOrEmpty(Name))
            {
                throw new CommandLineException(NuGetResources.SourcesCommandNameRequired);
            }

            List<PackageSource> sourceList = _sourceProvider.LoadPackageSources().ToList();
            int existingSourceIndex = sourceList.FindIndex(ps => Name.Equals(ps.Name, StringComparison.OrdinalIgnoreCase));
            if (existingSourceIndex == -1)
            {
                throw new CommandLineException(NuGetResources.SourcesCommandNoMatchingSourcesFound, Name);
            }
            var existingSource = sourceList[existingSourceIndex];

            if (!String.IsNullOrEmpty(Source) && !existingSource.Source.Equals(Source, StringComparison.OrdinalIgnoreCase))
            {
                if (!PathValidator.IsValidSource(Source))
                {
                    throw new CommandLineException(NuGetResources.SourcesCommandInvalidSource);
                }

                // If the user is updating the source, verify we don't have a duplicate.
                bool duplicateSource = sourceList.Any(ps => String.Equals(Source, ps.Source, StringComparison.OrdinalIgnoreCase));
                if (duplicateSource)
                {
                    throw new CommandLineException(NuGetResources.SourcesCommandUniqueSource);
                }
                existingSource = new PackageSource(Source, existingSource.Name);
            }

            ValidateCredentials();

            sourceList.RemoveAt(existingSourceIndex);
            existingSource.UserName = UserName;
            existingSource.Password = Password;
            
            sourceList.Insert(existingSourceIndex, existingSource);
            _sourceProvider.SavePackageSources(sourceList);
            Console.WriteLine(NuGetResources.SourcesCommandUpdateSuccessful, Name);
        }

        private void ValidateCredentials()
        {
            bool userNameEmpty = String.IsNullOrEmpty(UserName);
            bool passwordEmpty = String.IsNullOrEmpty(Password);

            if (userNameEmpty ^ passwordEmpty)
            {
                // If only one of them is set, throw.
                throw new CommandLineException(NuGetResources.SourcesCommandCredentialsRequired);
            }
        }

        private void PrintRegisteredSources()
        {
            var sourcesList = _sourceProvider.LoadPackageSources().ToList();
            if (!sourcesList.Any())
            {
                Console.WriteLine(NuGetResources.SourcesCommandNoSources);
                return;
            }
            Console.PrintJustified(0, NuGetResources.SourcesCommandRegisteredSources);
            Console.WriteLine();
            var sourcePadding = new String(' ', 6);
            for (int i = 0; i < sourcesList.Count; i++)
            {
                var source = sourcesList[i];
                var indexNumber = i + 1;
                var namePadding = new String(' ', i >= 9 ? 1 : 2);
                Console.WriteLine(
                    "  {0}.{1}{2} [{3}]",
                    indexNumber,
                    namePadding,
                    source.Name,
                    source.IsEnabled ? NuGetResources.SourcesCommandEnabled : NuGetResources.SourcesCommandDisabled);
                Console.WriteLine("{0}{1}", sourcePadding, source.Source);
            }
        }
    }
}
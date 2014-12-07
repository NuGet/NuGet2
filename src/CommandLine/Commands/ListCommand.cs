using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommand), "list", "ListCommandDescription",
        UsageSummaryResourceName = "ListCommandUsageSummary", UsageDescriptionResourceName = "ListCommandUsageDescription",
        UsageExampleResourceName = "ListCommandUsageExamples")]
    public class ListCommand : Command
    {
        private readonly List<string> _sources = new List<string>();

        [Option(typeof(NuGetCommand), "ListCommandSourceDescription")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option(typeof(NuGetCommand), "ListCommandVerboseListDescription")]
        public bool Verbose { get; set; }

        [Option(typeof(NuGetCommand), "ListCommandAllVersionsDescription")]
        public bool AllVersions { get; set; }

        [Option(typeof(NuGetCommand), "ListCommandPrerelease")]
        public bool Prerelease { get; set; }

        [Option(typeof(NuGetCommand), "ListCommandIncludeDelisted")]
        public bool IncludeDelisted { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call is expensive")]
        public IEnumerable<IPackage> GetPackages()
        {
            IPackageRepository packageRepository = GetRepository();
            string searchTerm = Arguments != null ? Arguments.FirstOrDefault() : null;

            IQueryable<IPackage> packages = packageRepository.Search(
                searchTerm,
                targetFrameworks: Enumerable.Empty<string>(),
                allowPrereleaseVersions: Prerelease,
                includeDelisted: IncludeDelisted);
            if (AllVersions)
            {
                return packages.OrderBy(p => p.Id);
            }
            else
            {
                if (Prerelease && packageRepository.SupportsPrereleasePackages)
                {
                    packages = packages.Where(p => p.IsAbsoluteLatestVersion);
                }
                else
                {
                    packages = packages.Where(p => p.IsLatestVersion);
                }
            }

            var result = packages.OrderBy(p => p.Id)
                .AsEnumerable();

            // we still need to do client side filtering of delisted & prerelease packages.
            if (IncludeDelisted == false)
            {
                result = result.Where(PackageExtensions.IsListed);
            }
            return result.Where(p => Prerelease || p.IsReleaseVersion())
                       .AsCollapsed();
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }

        public override void ExecuteCommand()
        {
            if (Verbose)
            {
                Console.WriteWarning(LocalizedResourceManager.GetString("Option_VerboseDeprecated"));
                Verbosity = Verbosity.Detailed;
            }

            IEnumerable<IPackage> packages = GetPackages();

            bool hasPackages = false;

            if (packages != null)
            {
                if (Verbosity == Verbosity.Detailed)
                {
                    /***********************************************
                     * Package-Name
                     *  1.0.0.2010
                     *  This is the package Description
                     * 
                     * Package-Name-Two
                     *  2.0.0.2010
                     *  This is the second package Description
                     ***********************************************/
                    foreach (var p in packages)
                    {
                        Console.PrintJustified(0, p.Id);
                        Console.PrintJustified(1, p.Version.ToString());
                        Console.PrintJustified(1, p.Description);
                        if (p.LicenseUrl != null && !string.IsNullOrEmpty(p.LicenseUrl.OriginalString))
                        {
                            Console.PrintJustified(1, 
                                String.Format(CultureInfo.InvariantCulture, LocalizedResourceManager.GetString("ListCommand_LicenseUrl"), p.LicenseUrl.OriginalString));
                        }
                        Console.WriteLine();
                        hasPackages = true;
                    }
                }
                else
                {
                    /***********************************************
                     * Package-Name 1.0.0.2010
                     * Package-Name-Two 2.0.0.2010
                     ***********************************************/
                    foreach (var p in packages)
                    {
                        Console.PrintJustified(0, p.GetFullName());
                        hasPackages = true;
                    }
                }
            }

            if (!hasPackages)
            {
                Console.WriteLine(LocalizedResourceManager.GetString("ListCommandNoPackages"));
            }
        }
    }
}
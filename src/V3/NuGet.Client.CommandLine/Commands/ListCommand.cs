using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using NuGet.Common;
using NuGet.Client;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommandResourceType), "list", "ListCommandDescription",
        UsageSummaryResourceName = "ListCommandUsageSummary", UsageDescriptionResourceName = "ListCommandUsageDescription",
        UsageExampleResourceName = "ListCommandUsageExamples")]
    public class ListCommand : Command
    {
        private readonly List<string> _sources = new List<string>();
       
        [Option(typeof(NuGetCommandResourceType), "ListCommandSourceDescription")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option(typeof(NuGetCommandResourceType), "ListCommandVerboseListDescription")]
        public bool Verbose { get; set; }

        [Option(typeof(NuGetCommandResourceType), "ListCommandAllVersionsDescription")]
        public bool AllVersions { get; set; }

        [Option(typeof(NuGetCommandResourceType), "ListCommandPrerelease")]
        public bool Prerelease { get; set; }

        public override void ExecuteCommand()
        {
            if (Verbose)
            {
                Console.WriteWarning(LocalizedResourceManager.GetString("Option_VerboseDeprecated"));
                Verbosity = Verbosity.Detailed;
            }

            int page = 100;
            int skip = 0;
            IEnumerable<JObject> packages = null;
            do
            {
                packages = GetPackages(skip, page).Result;
                skip += page;
                if (packages.Count() == 0) break;
                PrintPackages(packages);
            }
            while (true);
        }

        private async Task<IEnumerable<JObject>> GetPackages(int skip, int page)
        {
            SourceRepository sourceRepository = SourceRepositoryHelper.CreateSourceRepository(SourceProvider, Source);

            string searchTerm = Arguments != null ? Arguments.FirstOrDefault() : null;

            var Packages = await sourceRepository.Search(searchTerm, new SearchFilter() { IncludePrerelease = Prerelease, SupportedFrameworks = new FrameworkName[0] }, skip, page, CancellationToken.None);

            return Packages;
        }

        private void PrintPackages(IEnumerable<JObject> packages)
        {
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
                        if (AllVersions)
                        {
                            JArray versions = (JArray)p[Properties.Versions];
                            foreach (var version in versions)
                            {
                                Console.PrintJustified(0, p[Properties.PackageId].ToString());
                                Console.PrintJustified(1, version.ToString());
                                Console.PrintJustified(1, p[Properties.Summary].ToString());
                            }
                        }
                        else
                        {
                            Console.PrintJustified(0, p[Properties.PackageId].ToString());
                            Console.PrintJustified(1, p[Properties.LatestVersion].ToString());
                            Console.PrintJustified(1, p[Properties.Summary].ToString());
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
                        if (AllVersions)
                        {
                            JArray versions = (JArray)p[Properties.Versions];
                            foreach (var version in versions)
                            {
                                Console.PrintJustified(0, p[Properties.PackageId].ToString() + " " + version.ToString());
                            }
                        }
                        else
                        {
                            Console.PrintJustified(0, p[Properties.PackageId].ToString() + " " + p[Properties.LatestVersion].ToString());
                        }
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
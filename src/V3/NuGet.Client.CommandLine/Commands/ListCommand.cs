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

        [Option(typeof(NuGetCommandResourceType), "ListCommandAllVersionsDescription")]
        public bool AllVersions { get; set; }

        [Option(typeof(NuGetCommandResourceType), "ListCommandPrerelease")]
        public bool Prerelease { get; set; }

        public override void ExecuteCommand()
        {
            int page = 100;
            int skip = 0;
            IEnumerable<JObject> packages = null;
            do
            {
                packages = GetPackages(skip, page).Result;
                skip += page;
                if (!packages.Any()) break;
                PrintPackages(packages);
            }
            while (true);
        }

        private async Task<IEnumerable<JObject>> GetPackages(int skip, int page)
        {
            SourceRepository sourceRepository = SourceRepositoryHelper.CreateSourceRepository(SourceProvider, Source);

            string searchTerm = Arguments != null ? Arguments.FirstOrDefault() : null;

            var packages = await sourceRepository.Search(searchTerm, new SearchFilter() { IncludePrerelease = Prerelease, SupportedFrameworks = new FrameworkName[0] }, skip, page, CancellationToken.None);

            return packages;
        }

        private void PrintPackages(IEnumerable<JObject> packages)
        {
            Action<string, string, JObject> funcPrintPackage = Verbosity == Verbosity.Detailed ? (Action<string, string, JObject>)PrintPackageDetailed : PrintPackage;

            if (packages != null && packages.Any())
            {
                foreach (var p in packages)
                {
                    if (AllVersions)
                    {
                        JArray versions = (JArray)p[Properties.Versions];
                        foreach (var version in versions)
                        {
                            funcPrintPackage(p[Properties.PackageId].ToString(), version.ToString(), p);
                        }
                    }
                    else
                    {
                        funcPrintPackage(p[Properties.PackageId].ToString(), p[Properties.LatestVersion].ToString(), p);
                    }    
                }
            }
            else
            {
                Console.WriteLine(LocalizedResourceManager.GetString("ListCommandNoPackages"));
            }

        }

        private void PrintPackageDetailed(string packageId, string version, JObject package)
        {
            /***********************************************
            * Package-Name
            *  1.0.0.2010
            *  This is the package Summary
            * 
            * Package-Name-Two
            *  2.0.0.2010
            *  This is the second package Summary
            ***********************************************/
            Console.PrintJustified(0, packageId);
            Console.PrintJustified(1, version);
            Console.PrintJustified(1, package[Properties.Summary].ToString()?? string.Empty);
            Console.WriteLine();
        }

        private void PrintPackage(string packageId, string version, JObject package)
        {
            /***********************************************
            * Package-Name 1.0.0.2010
            * Package-Name-Two 2.0.0.2010
            ***********************************************/
            Console.PrintJustified(0, packageId + " " + version);
        }
    }
}
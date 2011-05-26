using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "list", "ListCommandDescription",
        UsageSummaryResourceName = "ListCommandUsageSummary", UsageDescriptionResourceName = "ListCommandUsageDescription")]
    public class ListCommand : Command {
        private readonly List<string> _sources = new List<string>();

        [Option(typeof(NuGetResources), "ListCommandSourceDescription")]
        public List<string> Source {
            get { return _sources; }
        }

        [Option(typeof(NuGetResources), "ListCommandVerboseListDescription")]
        public bool Verbose { get; set; }

        [Option(typeof(NuGetResources), "ListCommandAllVersionsDescription")]
        public bool AllVersions { get; set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        [ImportingConstructor]
        public ListCommand(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider) {
            if (packageRepositoryFactory == null) {
                throw new ArgumentNullException("packageRepositoryFactory");
            }

            if (sourceProvider == null) {
                throw new ArgumentNullException("sourceProvider");
            }

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public IEnumerable<IPackage> GetPackages() {
            IPackageRepository packageRepository = GetRepository();
            IQueryable<IPackage> packages = packageRepository.GetPackages().OrderBy(p => p.Id);
            if (Arguments != null && Arguments.Any()) {
                packages = packages.Find(Arguments.ToArray());
            }
            if (AllVersions) {
                // Do not collapse versions
                return packages;
            }
            return packages.DistinctLast(PackageEqualityComparer.Id, PackageComparer.Version);
        }

        private IPackageRepository GetRepository() {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }

        public override void ExecuteCommand() {

            IEnumerable<IPackage> packages = GetPackages();

            bool hasPackages = false;

            if (packages != null) {
                if (Verbose) {
                    /***********************************************
                     * Package-Name
                     *  1.0.0.2010
                     *  This is the package Description
                     * 
                     * Package-Name-Two
                     *  2.0.0.2010
                     *  This is the second package Description
                     ***********************************************/
                    foreach (var p in packages) {
                        Console.PrintJustified(0, p.Id);
                        Console.PrintJustified(1, p.Version.ToString());
                        Console.PrintJustified(1, p.Description);
                        Console.WriteLine();
                        hasPackages = true;
                    }
                }
                else {
                    /***********************************************
                     * Package-Name 1.0.0.2010
                     * Package-Name-Two 2.0.0.2010
                     ***********************************************/
                    foreach (var p in packages) {
                        Console.PrintJustified(0, p.GetFullName());
                        hasPackages = true;
                    }
                }
            }

            if (!hasPackages) {
                Console.WriteLine(NuGetResources.ListCommandNoPackages);
            }
        }
    }
}
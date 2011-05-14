using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet {
    public class AggregateRepository : PackageRepositoryBase, IPackageLookup {
        private readonly IEnumerable<IPackageRepository> _repositories;
        private const string SourceValue = "(Aggregate source)";

        public override string Source {
            get {
                return SourceValue;
            }
        }

        public bool IgnoreFailingRepositories { get; set; }

        public IEnumerable<IPackageRepository> Repositories {
            get { return _repositories; }
        }

        public AggregateRepository(IEnumerable<IPackageRepository> repositories) {
            if (repositories == null) {
                throw new ArgumentNullException("repositories");
            }
            _repositories = repositories;
        }

        public override IQueryable<IPackage> GetPackages() {
            return new AggregateQuery<IPackage>(_repositories.Select(r => r.GetPackages()),
                PackageEqualityComparer.IdAndVersion, IgnoreFailingRepositories);
        }

        public IPackage FindPackage(string packageId, Version version) {
            // When we're looking for an exact package, we can optimize but searching each
            // repository one by one until we find the package that matches.
            return Repositories.Select(r => r.FindPackage(packageId, version))
                               .FirstOrDefault(p => p != null);
                        
        }
    }
}

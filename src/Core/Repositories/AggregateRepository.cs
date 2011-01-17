using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet {
    public class AggregateRepository : PackageRepositoryBase {
        private readonly IEnumerable<IPackageRepository> _repositories;
        private const string SourceValue = "(Aggregate source)";

        public override string Source {
            get {
                return SourceValue;
            }
        }

        public AggregateRepository(IEnumerable<IPackageRepository> repositories) {
            if (repositories == null) {
                throw new ArgumentNullException("repositories");
            }
            _repositories = repositories;
        }

        public override IQueryable<IPackage> GetPackages() {
            return new AggregateQuery<IPackage>(_repositories.Select(r => r.GetPackages()), PackageEqualityComparer.IdAndVersion);
        }
    }
}

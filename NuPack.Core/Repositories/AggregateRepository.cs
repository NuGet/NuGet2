using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet {
    public class AggregateRepository : PackageRepositoryBase {
        private readonly IEnumerable<IPackageRepository> _repositories;
        private readonly IPackageRepository _single;

        public AggregateRepository(IEnumerable<IPackageRepository> repositories) {
            if (repositories == null) {
                throw new ArgumentNullException("repositories");
            }
            _repositories = repositories;
            _single = _repositories.SingleOrNull();
        }

        public override IQueryable<IPackage> GetPackages() {
            // Small optimization, if there is only one repository then we just delegate to that.
            if (_single != null) {
                return _single.GetPackages();
            }
            return new AggregateQuery<IPackage>(_repositories.Select(r => r.GetPackages()), PackageEqualityComparer.IdAndVersion);
        }
    }
}

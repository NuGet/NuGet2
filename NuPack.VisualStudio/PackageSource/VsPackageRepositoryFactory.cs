using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace NuGet.VisualStudio {
    [Export]
    public class VsPackageRepositoryFactory : IPackageRepositoryFactory {
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepositoryFactory _repositoryFactory;

        [ImportingConstructor]
        public VsPackageRepositoryFactory(IPackageSourceProvider packageSourceProvider)
            : this(PackageRepositoryFactory.Default, packageSourceProvider) {
        }

        public VsPackageRepositoryFactory(IPackageRepositoryFactory repositoryFactory, IPackageSourceProvider packageSourceProvider) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        public IPackageRepository CreateRepository(PackageSource packageSource) {
            if (packageSource.IsAggregate) {
                return new AggregateRepository(_packageSourceProvider.GetPackageSources()
                                                                     .Where(source => !source.IsAggregate)
                                                                     .Select(_repositoryFactory.CreateRepository));
            }
            return _repositoryFactory.CreateRepository(packageSource);
        }
    }
}

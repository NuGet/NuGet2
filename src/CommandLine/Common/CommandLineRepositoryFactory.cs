using System;
using System.Linq;

namespace NuGet.Common {
    public class CommandLineRepositoryFactory : IPackageRepositoryFactory {
        private readonly IPackageRepositoryFactory _repositoryFactory;

        public CommandLineRepositoryFactory()
            : this(PackageRepositoryFactory.Default) {
        }

        public CommandLineRepositoryFactory(IPackageRepositoryFactory repositoryFactory) {
            _repositoryFactory = repositoryFactory;
        }

        public IPackageRepository CreateRepository(PackageSource packageSource) {
            return new LazyRepository(_repositoryFactory, packageSource);
        }

        private class LazyRepository : IPackageRepository {            
            private readonly Lazy<IPackageRepository> _repository;

            public LazyRepository(IPackageRepositoryFactory repositoryFactory, PackageSource packageSource) {
                _repository = new Lazy<IPackageRepository>(() => repositoryFactory.CreateRepository(packageSource));
            }

            public string Source {
                get { return _repository.Value.Source; }
            }

            public IQueryable<IPackage> GetPackages() {
                return _repository.Value.GetPackages();
            }

            public void AddPackage(IPackage package) {
                _repository.Value.AddPackage(package);
            }

            public void RemovePackage(IPackage package) {
                _repository.Value.RemovePackage(package);
            }
        }
    }
}
using System;
using System.Linq;

namespace NuGet.Dialog.Providers {
    internal class LazyRespository : IPackageRepository {

        private Lazy<IPackageRepository> _repository;

        public LazyRespository(IPackageRepositoryFactory factory, PackageSource source) {
            _repository = new Lazy<IPackageRepository>(() => factory.CreateRepository(source));
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

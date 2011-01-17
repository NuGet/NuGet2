using System;
using System.Linq;

namespace NuGet.Dialog.Providers {
    internal class LazyRepository : IPackageRepository {

        private readonly Lazy<IPackageRepository> _repository;

        private IPackageRepository Repository {
            get { 
                return _repository.Value; 
            }
        }

        public string Source {
            get {
                return Repository.Source;
            }
        }

        public LazyRepository(IPackageRepositoryFactory factory, PackageSource source) {
            _repository = new Lazy<IPackageRepository>(() => factory.CreateRepository(source));
        }

        public IQueryable<IPackage> GetPackages() {
            return Repository.GetPackages();
        }

        public void AddPackage(IPackage package) {
            Repository.AddPackage(package);
        }

        public void RemovePackage(IPackage package) {
            Repository.RemovePackage(package);
        }
    }
}
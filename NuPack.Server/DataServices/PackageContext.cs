using System.Linq;

namespace NuGet.Server.DataServices {
    public class PackageContext {
        private IPackageRepository _repository;
        public PackageContext(IPackageRepository repository) {
            _repository = repository;
        }

        public IQueryable<Package> Packages {
            get {
                return from package in _repository.GetPackages()
                       select new Package(package);
            }
        }
    }
}

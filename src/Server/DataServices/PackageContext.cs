using System.Linq;
using NuGet.Server.Infrastructure;

namespace NuGet.Server.DataServices
{
    public class PackageContext
    {
        private readonly IServerPackageRepository _repository;
        public PackageContext(IServerPackageRepository repository)
        {
            _repository = repository;
        }

        public IQueryable<Package> Packages
        {
            get
            {
                var packages = from p in _repository.GetPackages()
                               select _repository.GetMetadataPackage(p);
                return packages.InterceptWith(new PackageIdComparisonVisitor());
            }
        }
    }
}

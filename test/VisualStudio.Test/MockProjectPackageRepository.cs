using System.Linq;
using NuGet.Test.Mocks;

namespace NuGet.VisualStudio.Test
{
    // This repository better simulates what happens when we're running the package manager in vs
    internal sealed class MockProjectPackageRepository : MockPackageRepository
    {
        private readonly IPackageRepository _parent;
        public MockProjectPackageRepository(IPackageRepository parent)
        {
            _parent = parent;
        }
        public override IQueryable<IPackage> GetPackages()
        {
            return base.GetPackages().Where(p => _parent.Exists(p));
        }
    }
}

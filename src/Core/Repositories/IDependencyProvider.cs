using System.Linq;

namespace NuGet {
    public interface IDependencyProvider {
        IQueryable<IPackage> GetDependencies(string packageId);
    }
}

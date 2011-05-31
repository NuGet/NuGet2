using System.Collections.Generic;

namespace NuGet {
    public interface IDependencyProvider {
        IEnumerable<IPackage> GetDependencies(string packageId);
    }
}

using System.Collections.Generic;

namespace NuPack {
    public interface IPackageOperationResolver {
        IEnumerable<PackageOperation> ResolveOperations(IPackage package);
    }
}

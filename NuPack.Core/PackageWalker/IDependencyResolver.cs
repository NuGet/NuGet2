namespace NuPack {
    using System.Collections.Generic;

    public interface IDependencyResolver {
        IEnumerable<IPackage> ResolveDependencies(IPackage package);
    }
}

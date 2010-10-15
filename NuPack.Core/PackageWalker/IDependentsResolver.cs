using System;
using System.Collections.Generic;

namespace NuPack {
    public interface IDependentsResolver {
        IEnumerable<IPackage> GetDependents(IPackage package);
    }
}

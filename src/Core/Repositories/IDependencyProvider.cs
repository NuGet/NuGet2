using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet {
    public interface IDependencyProvider {
        IQueryable<IPackage> GetDependencies(string packageId);
    }
}

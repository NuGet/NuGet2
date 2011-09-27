using System;

namespace NuGet {
    internal interface IPackageLookup {
        IPackage FindPackage(string packageId, SemanticVersion version);
    }
}

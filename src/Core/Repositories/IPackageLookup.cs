using System;

namespace NuGet {
    internal interface IPackageLookup {
        IPackage FindPackage(string packageId, SemVer version);
    }
}

using System;
using System.Globalization;
using Microsoft.Internal.Web.Utils;
using NuGet.Resources;

namespace NuGet {
    public static class PackageHelper {
        public static IPackage ResolvePackage(IPackageRepository sourceRepository, IPackageRepository localRepository, string packageId, Version version) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage package = null;

            if (version != null) {
                package = localRepository.FindPackage(packageId, version);
            }

            if (package == null) {
                package = sourceRepository.FindPackage(packageId, version: version);

                // If we already have this package installed, use the local copy so we don't 
                // end up using the one from the source repository
                if (package != null) {
                    package = localRepository.FindPackage(package.Id, package.Version) ?? package;
                }
            }

            if (package == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.UnknownPackage, packageId));
            }

            return package;
        }
    }
}

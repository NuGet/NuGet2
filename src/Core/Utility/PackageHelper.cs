using System;
using System.Globalization;
using Microsoft.Internal.Web.Utils;
using NuGet.Resources;

namespace NuGet {
    public static class PackageHelper {
        public static IPackage ResolvePackage(IPackageRepository sourceRepository, IPackageRepository localRepository, string packageId, SemVer version, bool allowPrereleaseVersions) {
            return ResolvePackage(sourceRepository, localRepository, constraintProvider: NullConstraintProvider.Instance, packageId: packageId, version: version, allowPrereleaseVersions: allowPrereleaseVersions);
        }

        public static IPackage ResolvePackage(IPackageRepository sourceRepository, IPackageRepository localRepository, IPackageConstraintProvider constraintProvider, 
            string packageId, SemVer version, bool allowPrereleaseVersions) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage package = null;

            // If we're looking for an exact version of a package then try local first
            if (version != null) {
                package = localRepository.FindPackage(packageId, version, allowPrereleaseVersions);
            }

            if (package == null) {
                // Try to find it in the source (regardless of version)
                // We use resolve package here since we want to take any constraints into account
                package = sourceRepository.FindPackage(packageId, version, constraintProvider, allowPrereleaseVersions);

                // If we already have this package installed, use the local copy so we don't 
                // end up using the one from the source repository
                if (package != null) {
                    package = localRepository.FindPackage(package.Id, package.Version, allowPrereleaseVersions) ?? package;
                }
            }

            // We still didn't find it so throw
            if (package == null) {
                if (version != null) {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackageSpecificVersion, packageId, version));
                }
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.UnknownPackage, packageId));
            }

            return package;
        }
    }
}

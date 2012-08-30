using System;
using System.Globalization;
using NuGet.Resources;

namespace NuGet
{
    public static class PackageHelper
    {
        public static IPackage ResolvePackage(IPackageRepository sourceRepository, IPackageRepository localRepository, string packageId, SemanticVersion version, bool allowPrereleaseVersions)
        {
            return ResolvePackage(sourceRepository, localRepository, constraintProvider: NullConstraintProvider.Instance, packageId: packageId, version: version, allowPrereleaseVersions: allowPrereleaseVersions);
        }

        public static IPackage ResolvePackage(IPackageRepository sourceRepository, IPackageRepository localRepository, IPackageConstraintProvider constraintProvider,
            string packageId, SemanticVersion version, bool allowPrereleaseVersions)
        {
            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage package = null;

            // If we're looking for an exact version of a package then try local first
            if (version != null)
            {
                package = localRepository.FindPackage(packageId, version, allowPrereleaseVersions, allowUnlisted: true);
            }

            if (package == null)
            {
                // Try to find it in the source (regardless of version)
                // We use resolve package here since we want to take any constraints into account
                package = sourceRepository.FindPackage(packageId, version, constraintProvider, allowPrereleaseVersions, allowUnlisted: false);

                // If we already have this package installed, use the local copy so we don't 
                // end up using the one from the source repository
                if (package != null)
                {
                    package = localRepository.FindPackage(package.Id, package.Version, allowPrereleaseVersions, allowUnlisted: true) ?? package;
                }
            }

            // We still didn't find it so throw
            if (package == null)
            {
                if (version != null)
                {
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

        /// <summary>
        /// Finds a package from the source repository that matches the id and version. 
        /// </summary>
        /// <param name="repository">The repository to find the package in.</param>
        /// <param name="packageId">Id of the package to find.</param>
        /// <param name="version">Version of the package to find.</param>
        /// <exception cref="System.InvalidOperationException">If the specified package cannot be found in the repository.</exception>
        public static IPackage ResolvePackage(IPackageRepository repository, string packageId, SemanticVersion version)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            var package = repository.FindPackage(packageId, version);
            if (package == null)
            {
                throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackageSpecificVersion, packageId, version));
            }

            return package;
        }
    }
}

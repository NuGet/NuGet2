using Newtonsoft.Json.Linq;
using NuGet.Resources;
using NuGet.Versioning;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public static class PackageRepositoryHelper
    {
        /// <summary>
        /// Resolve package from online and local repository
        /// Used for Install-Package and Update-Package command to verify the specified package version exists in the repo.
        /// </summary>
        /// <param name="sourceRepository"></param>
        /// <param name="localRepository"></param>
        /// <param name="identity"></param>
        /// <param name="allowPrereleaseVersions"></param>
        /// <returns></returns>
        public static PackageIdentity ResolvePackage(SourceRepository sourceRepository, IPackageRepository localRepository,
            PackageIdentity identity, bool allowPrereleaseVersions)
        {
            string packageId = identity.Id;
            NuGetVersion nVersion = identity.Version;
            string version = identity.Version.ToNormalizedString();

            if (String.IsNullOrEmpty(identity.Id))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            PackageIdentity resolvedIdentity = null;

            // If we're looking for an exact version of a package then try local first
            if (version != null)
            {
                SemanticVersion sVersion = new SemanticVersion(version);
                IPackage package = localRepository.FindPackage(packageId, sVersion, allowPrereleaseVersions, allowUnlisted: false);
                if (package != null)
                {
                    resolvedIdentity = new PackageIdentity(packageId, NuGetVersion.Parse(package.Version.ToString()));
                }
            }

            if (resolvedIdentity == null)
            {
                if (nVersion == null)
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackageSpecificVersion, packageId, version));
                }
                else
                {
                    Task<JObject> task = sourceRepository.GetPackageMetadata(packageId, nVersion);
                    JObject package = task.Result;
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
                    else
                    {
                        resolvedIdentity = new PackageIdentity(packageId, nVersion);
                    }
                }
            }

            return resolvedIdentity;
        }

        /// <summary>
        /// Resolve package from local repository
        /// Used for Uninstall-Package and Consolidate-Package command to verify the specified package version exists in the repo.
        /// </summary>
        /// <param name="localRepository"></param>
        /// <param name="identity"></param>
        /// <param name="allowPrereleaseVersions"></param>
        /// <returns></returns>
        public static PackageIdentity ResolvePackage(IPackageRepository localRepository, PackageIdentity identity, bool allowPrereleaseVersions)
        {
            string packageId = identity.Id;
            NuGetVersion nVersion = identity.Version;
            string version = identity.Version.ToNormalizedString();

            if (String.IsNullOrEmpty(identity.Id))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            PackageIdentity resolvedIdentity = null;

            // If we're looking for an exact version of a package then try local first
            if (version != null)
            {
                SemanticVersion sVersion = new SemanticVersion(version);
                IPackage package = localRepository.FindPackage(packageId, sVersion, allowPrereleaseVersions, allowUnlisted: false);
                if (package != null)
                {
                    resolvedIdentity = new PackageIdentity(packageId, NuGetVersion.Parse(package.Version.ToString()));
                }
            }

            if (resolvedIdentity == null)
            {
                if (version != null)
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackageSpecificVersion, packageId, version));
                }
                else
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackage, packageId));
                }
            }

            return resolvedIdentity;
        }
    }
}

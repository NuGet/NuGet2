using System;
using System.Globalization;
using NuGet.Resources;
using NuGet.Client;
using NuGet.Versioning;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace NuGet.Client
{
    // Resolve package from online and local repository
    // Used for Install-Package and Update-Package command to verify the specified package version exists in the repo.
    public static class PackageRepositoryHelper
    {
        public static PackageIdentity ResolvePackage(SourceRepository sourceRepository, IPackageRepository localRepository, string packageId, string version, bool allowPrereleaseVersions)
        {
            return ResolvePackage(sourceRepository, localRepository, constraintProvider: NullConstraintProvider.Instance, packageId: packageId, version: version, allowPrereleaseVersions: allowPrereleaseVersions);
        }

        public static PackageIdentity ResolvePackage(SourceRepository sourceRepository, IPackageRepository localRepository, IPackageConstraintProvider constraintProvider,
            string packageId, string version, bool allowPrereleaseVersions)
        {
            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            PackageIdentity identity = null;

            // If we're looking for an exact version of a package then try local first
            if (version != null)
            {
                SemanticVersion sVersion = new SemanticVersion(version);
                IPackage package = localRepository.FindPackage(packageId, sVersion, allowPrereleaseVersions, allowUnlisted: true);
                if (package != null)
                {
                    identity = new PackageIdentity(packageId, NuGetVersion.Parse(package.Version.ToString()));
                }
            }

            if (identity == null)
            {
                try
                {
                    // Serverside API - would be good to create a GetPackageMetadatByIdAndVersion method on V3SourceRepository
                    Task<IEnumerable<JObject>> packages = sourceRepository.GetPackageMetadataById(packageId);
                    var r = packages.Result;
                    var allVersions = r.Select(p => NuGetVersion.Parse(p.Value<string>(Properties.Version)));
                    NuGetVersion nVersion = allVersions.Where(q => q == NuGetVersion.Parse(version)).FirstOrDefault();
                    identity = new PackageIdentity(packageId, nVersion);
                }
                catch (NullReferenceException) { };
            }

            // We still didn't find it so throw
            if (identity == null)
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

            return identity;
        }

        // Resolve package from local repository only
        // Used for Uninstall-Package command to make sure the Pakcage is already installed to the project
        public static PackageIdentity ResolvePackage(IPackageRepository localRepository, string packageId, string version, bool allowPrereleaseVersions)
        {
            return ResolvePackage(localRepository, constraintProvider: NullConstraintProvider.Instance, packageId: packageId, version: version, allowPrereleaseVersions: allowPrereleaseVersions);
        }

        public static PackageIdentity ResolvePackage(IPackageRepository localRepository, IPackageConstraintProvider constraintProvider,
            string packageId, string version, bool allowPrereleaseVersions)
        {
            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            PackageIdentity identity = null;
            IPackage package = null;

            // If we're looking for an exact version 
            if (version != null)
            {
                SemanticVersion sVersion = new SemanticVersion(version);
                package = localRepository.FindPackage(packageId, sVersion, allowPrereleaseVersions, allowUnlisted: true);
            }
            else
            {
                IPackage tPackage = localRepository.FindPackage(packageId);
                if (tPackage != null)
                {
                    SemanticVersion tVersion = tPackage.Version;
                    package = localRepository.FindPackage(packageId, tVersion, allowPrereleaseVersions, allowUnlisted: true) ?? tPackage;
                }
            }

            if (package != null)
            {
                identity = new PackageIdentity(packageId, NuGetVersion.Parse(package.Version.ToString()));
            }

            // We still didn't find it so throw
            if (identity == null)
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

            return identity;
        }
    }
}

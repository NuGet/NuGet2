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
                identity = new PackageIdentity(packageId, NuGetVersion.Parse(package.Version.ToString()));
            }

            if (identity == null)
            {
                // Serverside API - would be good to create a GetPackageMetadatByIdAndVersion method on V3SourceRepository
                Task<IEnumerable<JObject>> packages = sourceRepository.GetPackageMetadataById(packageId);
                var r = packages.Result;
                var allVersions = r.Select(p => NuGetVersion.Parse(p.Value<string>(Properties.Version)));
                NuGetVersion nVersion = allVersions.Where(q => q == NuGetVersion.Parse(version)).FirstOrDefault();
                identity = new PackageIdentity(packageId, nVersion);

                // If we already have this package installed, use the local copy so we don't 
                // end up using the one from the source repository
                // Comment out for V3 as the return type is PackageIdentity
                //if (identity != null)
                //{
                //    identity = localRepository.FindPackage(identity.Id, identity.Version, allowPrereleaseVersions, allowUnlisted: true) ?? identity;
                //}
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

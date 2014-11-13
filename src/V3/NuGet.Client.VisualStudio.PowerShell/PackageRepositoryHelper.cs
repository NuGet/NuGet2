using Newtonsoft.Json.Linq;
using NuGet.Resources;
using NuGet.Versioning;
using System;
using System.Globalization;
using System.Threading.Tasks;

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
                    NuGetVersion nVersion = NuGetVersion.Parse(version);
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
                        identity = new PackageIdentity(packageId, nVersion);
                    }
                }
                catch (Exception e)
                { 
                    throw e;
                }
            }

            return identity;
        }
    }
}

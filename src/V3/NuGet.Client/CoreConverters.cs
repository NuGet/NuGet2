using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace NuGet.Client
{
    internal static class CoreConverters
    {
        internal static NuGetVersion SafeToNuGetVer(SemanticVersion semanticVersion)
        {
            if (semanticVersion == null)
            {
                return null;
            }
            return new NuGetVersion(
                semanticVersion.Version,
                semanticVersion.SpecialVersion);
        }

        internal static VersionRange SafeToVerRange(IVersionSpec spec)
        {
            if(spec == null) {
                return null;
            }
            return new VersionRange(
                SafeToNuGetVer(spec.MinVersion),
                spec.IsMinInclusive,
                SafeToNuGetVer(spec.MaxVersion),
                spec.IsMaxInclusive);
        }

        internal static PackageIdentity SafeToPackageIdentity(string id, SemanticVersion version)
        {
            return new PackageIdentity(id, SafeToNuGetVer(version));
        }

        internal static PackageIdentity SafeToPackageIdentity(IPackageName name)
        {
            if (name == null)
            {
                return null;
            }
            return SafeToPackageIdentity(name.Id, name.Version);
        }

        internal static InstalledPackageReference SafeToInstalledPackageReference(PackageReference packageRef)
        {
            if (packageRef == null)
            {
                return null;
            }
            return new InstalledPackageReference(
                    CoreConverters.SafeToPackageIdentity(packageRef.Id, packageRef.Version),
                    CoreConverters.SafeToVerRange(packageRef.VersionConstraint),
                    packageRef.TargetFramework,
                    packageRef.IsDevelopmentDependency,
                    packageRef.RequireReinstallation);
        }

        internal static NuGet.SemanticVersion SafeToSemVer(SimpleVersion ver)
        {
            if (ver == null)
            {
                return null;
            }
            NuGet.Versioning.SemanticVersion semVer = ver as NuGet.Versioning.SemanticVersion;
            if (semVer == null)
            {
                return new NuGet.SemanticVersion(ver.ToString());
            }
            NuGetVersion nugetVer = ver as NuGetVersion;
            if (nugetVer != null)
            {
                return new NuGet.SemanticVersion(nugetVer.Version, nugetVer.Release);
            }
            return new NuGet.SemanticVersion(new Version(semVer.Major, semVer.Minor, semVer.Patch), semVer.Release);
        }

        internal static IVersionSpec SafeToVerSpec(VersionRange versionRange)
        {
            if (versionRange == null)
            {
                return null;
            }

            return new VersionSpec()
            {
                IsMaxInclusive = versionRange.IsMaxInclusive,
                IsMinInclusive = versionRange.IsMinInclusive,
                MaxVersion = SafeToSemVer(versionRange.MaxVersion),
                MinVersion = SafeToSemVer(versionRange.MinVersion)
            };
        }
    }
}

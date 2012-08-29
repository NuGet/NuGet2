using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
    internal static class PackageUtility
    {
        public static bool IsManifest(string path)
        {
            return Path.GetExtension(path).Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPackageFile(string path)
        {
            return Path.GetExtension(path).Equals(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAssembly(string path)
        {
            return path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSatellitePackage(
            IPackage package, 
            IPackageRepository repository,
            FrameworkName targetFramework,
            out IPackage runtimePackage)
        {
            // A satellite package has the following properties:
            //     1) A package suffix that matches the package's language, with a dot preceding it
            //     2) A dependency on the package with the same Id minus the language suffix
            //     3) The dependency can be found by Id in the repository (as its path is needed for installation)
            // Example: foo.ja-jp, with a dependency on foo

            runtimePackage = null;

            if (package.IsSatellitePackage())
            {
                string runtimePackageId = package.Id.Substring(0, package.Id.Length - (package.Language.Length + 1));
                PackageDependency dependency = package.FindDependency(runtimePackageId, targetFramework);

                if (dependency != null)
                {
                    runtimePackage = repository.FindPackage(runtimePackageId, versionSpec: dependency.VersionSpec, allowPrereleaseVersions: true, allowUnlisted: true);
                }
            }

            return runtimePackage != null;
        }
    }
}

using System;

namespace NuGet {
    public interface IPackagePathResolver {
        /// <summary>
        /// Gets the physical installation path of a package
        /// </summary>
        string GetInstallPath(IPackage package);

        /// <summary>
        /// Gets the package directory name
        /// </summary>
        string GetPackageDirectory(IPackage package);

        string GetPackageDirectory(string packageId, Version version);

        /// <summary>
        /// Gets the package file name
        /// </summary>
        string GetPackageFileName(IPackage package);

        string GetPackageFileName(string packageId, Version version);
    }
}

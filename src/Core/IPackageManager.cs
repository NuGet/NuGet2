using System;

namespace NuGet
{
    public interface IPackageManager
    {
        /// <summary>
        /// File system used to perform local operations in.
        /// </summary>
        IFileSystem FileSystem { get; set; }

        /// <summary>
        /// Local repository to install and reference packages.
        /// </summary>
        IPackageRepository LocalRepository { get; }

        ILogger Logger { get; set; }

        DependencyVersion DependencyVersion { get; set; }

        bool WhatIf { get; set; }
        
        /// <summary>
        /// Remote repository to install packages from.
        /// </summary>
        IPackageRepository SourceRepository { get; }

        /// <summary>
        /// PathResolver used to determine paths for installed packages.
        /// </summary>
        IPackagePathResolver PathResolver { get; }

        event EventHandler<PackageOperationEventArgs> PackageInstalled;
        event EventHandler<PackageOperationEventArgs> PackageInstalling;
        event EventHandler<PackageOperationEventArgs> PackageUninstalled;
        event EventHandler<PackageOperationEventArgs> PackageUninstalling;
        void InstallPackage(IPackage package,FrameworkName targetFramework,bool ignoreDependencies,bool allowPrereleaseVersions,bool ignoreWalkInfo = false)
        void InstallPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions);
        void InstallPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions, bool ignoreWalkInfo);
        void InstallPackage(string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions);
        void UpdatePackage(IPackage newPackage, bool updateDependencies, bool allowPrereleaseVersions);
        void UpdatePackage(string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions);
        void UpdatePackage(string packageId, IVersionSpec versionSpec, bool updateDependencies, bool allowPrereleaseVersions);
        void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies);
        void UninstallPackage(string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies);
    }
}

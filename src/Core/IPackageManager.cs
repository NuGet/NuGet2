using System;
namespace NuGet {
    public interface IPackageManager {
        IFileSystem FileSystem { get; set; }
        IPackageRepository LocalRepository { get; }
        ILogger Logger { get; set; }
        IPackageRepository SourceRepository { get; }
        IPackagePathResolver PathResolver { get; }

        event EventHandler<PackageOperationEventArgs> PackageInstalled;
        event EventHandler<PackageOperationEventArgs> PackageInstalling;
        event EventHandler<PackageOperationEventArgs> PackageUninstalled;
        event EventHandler<PackageOperationEventArgs> PackageUninstalling;

        void InstallPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions);
        void InstallPackage(string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions);
        void UpdatePackage(IPackage newPackage, bool updateDependencies, bool allowPrereleaseVersions);
        void UpdatePackage(string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions);
        void UpdatePackage(string packageId, IVersionSpec versionSpec, bool updateDependencies, bool allowPrereleaseVersions);
        void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies);
        void UninstallPackage(string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies);
    }
}

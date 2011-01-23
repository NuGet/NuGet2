using System;
namespace NuGet {
    public interface IPackageManager {
        IFileSystem FileSystem { get; set; }
        IPackageRepository LocalRepository { get; }
        ILogger Logger { get; set; }
        IPackageRepository SourceRepository { get; }

        event EventHandler<PackageOperationEventArgs> PackageInstalled;
        event EventHandler<PackageOperationEventArgs> PackageInstalling;
        event EventHandler<PackageOperationEventArgs> PackageUninstalled;
        event EventHandler<PackageOperationEventArgs> PackageUninstalling;

        void InstallPackage(IPackage package, bool ignoreDependencies);
        void InstallPackage(string packageId, Version version, bool ignoreDependencies);
        void UpdatePackage(IPackage oldPackage, IPackage newPackage, bool updateDependencies);
        void UpdatePackage(string packageId, Version version, bool updateDependencies);
        void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies);
        void UninstallPackage(string packageId, Version version, bool forceRemove, bool removeDependencies);
    }
}

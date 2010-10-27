using System;
namespace NuGet {
    public interface IPackageManager {
        IFileSystem FileSystem { get; set; }
        IPackageRepository LocalRepository { get; }
        ILogger Logger { get; set; }
        IPackagePathResolver PathResolver { get; }
        IPackageRepository SourceRepository { get; }

        event EventHandler<PackageOperationEventArgs> PackageInstalled;
        event EventHandler<PackageOperationEventArgs> PackageInstalling;
        event EventHandler<PackageOperationEventArgs> PackageUninstalled;
        event EventHandler<PackageOperationEventArgs> PackageUninstalling;

        void InstallPackage(IPackage package, bool ignoreDependencies);
        void InstallPackage(string packageId);
        void InstallPackage(string packageId, Version version);
        void InstallPackage(string packageId, Version version, bool ignoreDependencies);                
        void UninstallPackage(IPackage package);
        void UninstallPackage(IPackage package, bool forceRemove);
        void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies);
        void UninstallPackage(string packageId);
        void UninstallPackage(string packageId, Version version);
        void UninstallPackage(string packageId, Version version, bool forceRemove);
        void UninstallPackage(string packageId, Version version, bool forceRemove, bool removeDependencies);
    }
}

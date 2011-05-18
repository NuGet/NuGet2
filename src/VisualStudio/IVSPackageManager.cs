using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.VisualStudio {
    public interface IVsPackageManager : IPackageManager {
        IProjectManager GetProjectManager(Project project);

        // Install
        void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies);
        void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies, ILogger logger);
        void InstallPackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, bool ignoreDependencies, ILogger logger);

        // Uninstall
        void UninstallPackage(IProjectManager projectManager, string packageId, Version version, bool forceRemove, bool removeDependencies);
        void UninstallPackage(IProjectManager projectManager, string packageId, Version version, bool forceRemove, bool removeDependencies, ILogger logger);

        // Update
        void UpdatePackages(ILogger logger);
        void UpdatePackages(ILogger logger, IPackageOperationEventListener packageOperationEventListener);
        void UpdatePackage(string packageId, Version version, bool updateDependencies, ILogger logger);
        void UpdatePackage(string packageId, Version version, bool updateDependencies, ILogger logger, IPackageOperationEventListener packageOperationEventListener);
        void UpdatePackage(string packageId, IVersionSpec versionSpec, bool updateDependencies, ILogger logger);
        void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies);
        void UpdatePackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, bool updateDependencies, ILogger logger);
        void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies, ILogger logger);

        // Safe update (only bug fixes)
        void SafeUpdatePackages(ILogger logger);
        void SafeUpdatePackage(string packageId, bool updateDependencies, ILogger logger);
        void SafeUpdatePackage(IProjectManager projectManager, string packageId, bool updateDependencies, ILogger logger);
    }
}
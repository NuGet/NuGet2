using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.VisualStudio {
    public interface IVsPackageManager : IPackageManager {
        bool IsProjectLevel(IPackage package);

        IProjectManager GetProjectManager(Project project);

        // Install
        void InstallPackage(IEnumerable<Project> projects, IPackage package, IEnumerable<PackageOperation> operations, bool ignoreDependencies, bool allowPrereleaseVersions, 
            ILogger logger, IPackageOperationEventListener eventListener);
        void InstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions, ILogger logger);
        void InstallPackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, bool ignoreDependencies, bool allowPrereleaseVersions, ILogger logger);

        // Uninstall
        void UninstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies);
        void UninstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies, ILogger logger);

        // Update
        void UpdatePackages(bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void UpdatePackages(IProjectManager projectManager, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);

        void UpdatePackage(IEnumerable<Project> projects, IPackage package, IEnumerable<PackageOperation> operations, bool updateDependencies, bool allowPrereleaseVersions, 
            ILogger logger, IPackageOperationEventListener eventListener);
        void UpdatePackage(string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void UpdatePackage(string packageId, IVersionSpec versionSpec, bool updateDependencies, bool allowPrereleaseVersions, 
            ILogger logger, IPackageOperationEventListener eventListener);
        void UpdatePackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, bool updateDependencies, bool allowPrereleaseVersions, 
            ILogger logger);
        void UpdatePackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);

        // Safe update (only bug fixes)
        void SafeUpdatePackages(bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void SafeUpdatePackages(IProjectManager projectManager, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);

        void SafeUpdatePackage(string packageId, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void SafeUpdatePackage(IProjectManager projectManager, string packageId, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);
    }
}
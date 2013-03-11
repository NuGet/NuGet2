using System.Collections.Generic;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public interface IVsPackageManager : IPackageManager
    {
        bool BindingRedirectEnabled { get; set; }

        bool IsProjectLevel(IPackage package);

        IProjectManager GetProjectManager(Project project);

        // Install
        void InstallPackage(IEnumerable<Project> projects, IPackage package, IEnumerable<PackageOperation> operations, bool ignoreDependencies, bool allowPrereleaseVersions,
            ILogger logger, IPackageOperationEventListener eventListener);
        void InstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions, ILogger logger);
        void InstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions, bool skipAssemblyReferences, ILogger logger);
        void InstallPackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, bool ignoreDependencies, bool allowPrereleaseVersions, ILogger logger);

        // Uninstall
        void UninstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies);
        void UninstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies, ILogger logger);

        // Update multiple packages
        void UpdatePackages(bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void UpdatePackages(IProjectManager projectManager, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);
        
        void UpdatePackages(IProjectManager projectManager, IEnumerable<IPackage> packages, IEnumerable<PackageOperation> operations,  
            bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);

        void UpdateSolutionPackages(IEnumerable<IPackage> packages, IEnumerable<PackageOperation> operations, 
            bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);

        // Update one package
        void UpdatePackage(string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void UpdatePackage(string packageId, IVersionSpec versionSpec, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void UpdatePackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);
        void UpdatePackage(IEnumerable<Project> projects, IPackage package, IEnumerable<PackageOperation> operations,
            bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);

        // Safe update (only bug fixes)
        void SafeUpdatePackages(bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void SafeUpdatePackages(IProjectManager projectManager, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);

        void SafeUpdatePackage(string packageId, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void SafeUpdatePackage(IProjectManager projectManager, string packageId, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);

        // Reinstall
        void ReinstallPackages(bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void ReinstallPackages(IProjectManager projectManager, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);

        void ReinstallPackage(string packageId, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger, IPackageOperationEventListener eventListener);
        void ReinstallPackage(IProjectManager projectManager, string packageId, bool updateDependencies, bool allowPrereleaseVersions, ILogger logger);
    }
}
using System;
using EnvDTE;

namespace NuGet.VisualStudio {
    public interface IVsPackageManager : IPackageManager {
        IProjectManager GetProjectManager(Project project);

        void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies);
        void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies, ILogger logger);
        void UninstallPackage(IProjectManager projectManager, string packageId, Version version, bool forceRemove, bool removeDependencies);
        void UninstallPackage(IProjectManager projectManager, string packageId, Version version, bool forceRemove, bool removeDependencies, ILogger logger);
        void UpdatePackage(IProjectManager projectManager, string id, Version version, bool updateDependencies);
        void UpdatePackage(IProjectManager projectManager, string id, Version version, bool updateDependencies, ILogger logger);
    }
}

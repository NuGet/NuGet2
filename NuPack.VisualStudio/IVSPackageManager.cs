using System;
using EnvDTE;

namespace NuPack.VisualStudio {
    public interface IVSPackageManager : IPackageManager {
        IProjectManager GetProjectManager(Project project);

        void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies, ILogger logger);
        void UninstallPackage(IProjectManager projectManager, string packageId, Version version, bool forceRemove, bool removeDependencies, ILogger logger);
        void UpdatePackage(IProjectManager projectManager, string id, Version version, bool updateDependencies, ILogger logger);
    }
}

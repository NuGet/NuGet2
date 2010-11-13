using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace NuGet.VisualStudio {
    public class VsPackageManager : PackageManager, IVsPackageManager {
        private readonly Dictionary<Project, IProjectManager> _projectManagers = null;
        private readonly ISharedPackageRepository _localRepository;

        public VsPackageManager(ISolutionManager solutionManager,
                                IPackageRepository sourceRepository,
                                IFileSystem fileSystem,
                                ISharedPackageRepository localRepository) :
            base(sourceRepository, new DefaultPackagePathResolver(fileSystem), fileSystem, localRepository) {

            _localRepository = localRepository;
            _projectManagers = solutionManager.GetProjects().ToDictionary(p => p, CreateProjectManager);
        }

        protected virtual IEnumerable<IProjectManager> ProjectManagers {
            get {
                return _projectManagers.Values;
            }
        }

        public virtual IProjectManager GetProjectManager(Project project) {
            IProjectManager projectManager;
            _projectManagers.TryGetValue(project, out projectManager);
            return projectManager;
        }

        public void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies) {
            InstallPackage(projectManager, packageId, version, ignoreDependencies, NullLogger.Instance);
        }

        public virtual void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies, ILogger logger) {
            InitializeLogger(logger, projectManager);

            // REVIEW: This isn't transactional, so if add package reference fails
            // the user has to manually clean it up by uninstalling it
            InstallPackage(packageId, version, ignoreDependencies);

            if (projectManager != null) {
                projectManager.AddPackageReference(packageId, version, ignoreDependencies);
            }
        }

        public void UninstallPackage(IProjectManager projectManager, string packageId, Version version, bool forceRemove, bool removeDependencies) {
            UninstallPackage(projectManager, packageId, version, forceRemove, removeDependencies, NullLogger.Instance);
        }

        public virtual void UninstallPackage(IProjectManager projectManager, string packageId, Version version, bool forceRemove, bool removeDependencies, ILogger logger) {
            InitializeLogger(logger, projectManager);

            // If we've specified a version then we've probably trying to remove a specific version of
            // a solution level package (since we allow side by side there)
            if (projectManager != null && projectManager.LocalRepository.Exists(packageId) && version == null) {
                projectManager.RemovePackageReference(packageId, forceRemove, removeDependencies);
            }

            UninstallPackage(packageId, version, forceRemove, removeDependencies);
        }

        public void UpdatePackage(IProjectManager projectManager, string id, Version version, bool updateDependencies) {
            UpdatePackage(projectManager, id, version, updateDependencies, NullLogger.Instance);
        }

        // REVIEW: Do we even need this method?
        public virtual void UpdatePackage(IProjectManager projectManager, string id, Version version, bool updateDependencies, ILogger logger) {
            InstallPackage(projectManager, id, version, !updateDependencies, logger);
        }

        protected override void ExecuteUninstall(IPackage package) {
            // Check if the package is in use before removing it
            if (!_localRepository.IsReferenced(package.Id, package.Version)) {
                base.ExecuteUninstall(package);
            }
        }

        private void InitializeLogger(ILogger logger, IProjectManager projectManager) {
            // Setup logging on all of our objects
            Logger = logger;
            FileSystem.Logger = logger;

            if (projectManager != null) {
                projectManager.Logger = logger;
                projectManager.Project.Logger = logger;
            }
        }

        private IProjectManager CreateProjectManager(Project project) {
            return new ProjectManager(_localRepository, PathResolver, VsProjectSystemFactory.CreateProjectSystem(project));
        }
    }
}

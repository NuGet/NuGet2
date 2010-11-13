using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace NuGet.VisualStudio {
    public class VsPackageManager : PackageManager, IVsPackageManager {
        private readonly ISharedPackageRepository _sharedRepository;

        public VsPackageManager(ISolutionManager solutionManager,
                                IPackageRepository sourceRepository,
                                IFileSystem fileSystem,
                                ISharedPackageRepository sharedRepository) :
            base(sourceRepository, new DefaultPackagePathResolver(fileSystem), fileSystem, sharedRepository) {

            _sharedRepository = sharedRepository;
        }

        public virtual IProjectManager GetProjectManager(Project project) {
            return CreateProjectManager(project);
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
            if (!_sharedRepository.IsReferenced(package.Id, package.Version)) {
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
            // Create the projet system
            IProjectSystem projectSystem = VsProjectSystemFactory.CreateProjectSystem(project);

            // Create the project manager with the shared repository
            return new ProjectManager(_sharedRepository, 
                                      PathResolver, 
                                      projectSystem, 
                                      new PackageReferenceRepository(projectSystem, _sharedRepository));
        }
    }
}

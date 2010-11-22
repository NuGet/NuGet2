using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnvDTE;

namespace NuGet.VisualStudio {
    public class VsPackageManager : PackageManager, IVsPackageManager {
        private readonly ISharedPackageRepository _sharedRepository;
        private readonly IDictionary<Project, IProjectManager> _projects;

        public VsPackageManager(ISolutionManager solutionManager,
                                IPackageRepository sourceRepository,
                                IFileSystem fileSystem,
                                ISharedPackageRepository sharedRepository) :
            base(sourceRepository, new DefaultPackagePathResolver(fileSystem), fileSystem, sharedRepository) {

            _sharedRepository = sharedRepository;
            _projects = solutionManager.GetProjects().ToDictionary(p => p, CreateProjectManager);
        }

        public virtual IProjectManager GetProjectManager(Project project) {
            IProjectManager projectManager;
            _projects.TryGetValue(project, out projectManager);
            return projectManager;
        }

        private IProjectManager CreateProjectManager(Project project) {
            // Create the projet system
            IProjectSystem projectSystem = VsProjectSystemFactory.CreateProjectSystem(project);

            var repository = new PackageReferenceRepository(projectSystem, _sharedRepository);

            // Ensure the logger is null while registering the repository
            FileSystem.Logger = null;
            Logger = null;

            // Ensure that this repository is registered with the shared repository if it needs to be
            repository.RegisterIfNecessary();

            // Create the project manager with the shared repository
            return new ProjectManager(_sharedRepository, PathResolver, projectSystem, repository);
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
                projectManager.PackageReferenceRemoved += (sender, e) => {
                    if (LocalRepository.Exists(e.Package)) {
                        // Remove any packages that would be removed as a result of updating a dependency or the package itself
                        UninstallPackage(e.Package, forceRemove: true, removeDependencies: !ignoreDependencies);
                    }
                };

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

        public void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies) {
            UpdatePackage(projectManager, packageId, version, updateDependencies, NullLogger.Instance);
        }

        // REVIEW: Do we even need this method?
        public virtual void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies, ILogger logger) {
            InstallPackage(projectManager, packageId, version, !updateDependencies, logger);
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
    }
}

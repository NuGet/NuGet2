using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    public class VsPackageManager : PackageManager, IVsPackageManager {
        private readonly Dictionary<Project, IProjectManager> _projectManagers = null;

        public VsPackageManager(ISolutionManager solutionManager,
                                IPackageRepository sourceRepository,
                                IFileSystem fileSystem,
                                IPackageRepository localRepository) :
            base(sourceRepository, new DefaultPackagePathResolver(fileSystem), fileSystem, localRepository) {

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

            var projectsWithPackage = GetProjectsWithPackage(packageId, version);

            // If we've specified a version then we've probably trying to remove a specific version of
            // a solution level package (since we allow side by side there)
            if (projectManager != null && projectManager.LocalRepository.Exists(packageId) && version == null) {
                projectManager.RemovePackageReference(packageId, forceRemove, removeDependencies);

                if (!projectsWithPackage.Any()) {
                    UninstallPackage(packageId, version, forceRemove, removeDependencies);
                }
            }
            else if (!projectsWithPackage.Any()) {
                UninstallPackage(packageId, version, forceRemove, removeDependencies);
            }
            else {
                logger.Log(MessageLevel.Warning, VsResources.PackageCannotBeRemovedBecauseItIsInUse, packageId, String.Join(", ", projectsWithPackage.Select(p => p.Project.ProjectName)));
            }
        }

        public void UpdatePackage(IProjectManager projectManager, string id, Version version, bool updateDependencies) {
            UpdatePackage(projectManager, id, version, updateDependencies, NullLogger.Instance);
        }

        // REVIEW: Do we even need this method?
        public virtual void UpdatePackage(IProjectManager projectManager, string id, Version version, bool updateDependencies, ILogger logger) {
            InstallPackage(projectManager, id, version, !updateDependencies, logger);
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
            return new ProjectManager(LocalRepository, PathResolver, VsProjectSystemFactory.CreateProjectSystem(project));
        }

        private IEnumerable<IProjectManager> GetProjectsWithPackage(string packageId, Version version) {
            return from projectManager in ProjectManagers
                   let package = projectManager.LocalRepository.FindPackage(packageId)
                   where package != null && (version == null || (version != null && package.Version.Equals(version)))
                   select projectManager;
        }
    }
}

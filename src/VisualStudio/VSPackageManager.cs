using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    public class VsPackageManager : PackageManager, IVsPackageManager {
        private readonly ISharedPackageRepository _sharedRepository;
        private readonly IDictionary<Project, IProjectManager> _projects;
        private readonly IPackageRepository _recentPackagesRepository;

        public VsPackageManager(ISolutionManager solutionManager,
                                IPackageRepository sourceRepository,
                                IFileSystem fileSystem,
                                ISharedPackageRepository sharedRepository,
                                IPackageRepository recentPackagesRepository) :
            base(sourceRepository, new DefaultPackagePathResolver(fileSystem), fileSystem, sharedRepository) {

            _sharedRepository = sharedRepository;
            _projects = solutionManager.GetProjects().ToDictionary(p => p, CreateProjectManager);
            _recentPackagesRepository = recentPackagesRepository;
        }

        public VsPackageManager(ISolutionManager solutionManager,
                                IPackageRepository sourceRepository,
                                IFileSystem fileSystem,
                                ISharedPackageRepository sharedRepository) :
            this(solutionManager, sourceRepository, fileSystem, sharedRepository, null) {
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

            IPackage package = SourceRepository.FindPackage(packageId, version: version);

            if (package == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.UnknownPackage, packageId));
            }

            // REVIEW: This isn't transactional, so if add package reference fails
            // the user has to manually clean it up by uninstalling it
            InstallPackage(package, ignoreDependencies);

            AddPackageReference(projectManager, packageId, version, ignoreDependencies);

            // Add package to recent repository
            AddPackageToRecentRepository(package);
        }

        public void InstallPackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, bool ignoreDependencies, ILogger logger) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }

            if (operations == null) {
                throw new ArgumentNullException("operations");
            }

            ExecuteOperatonsWithPackage(projectManager, package, operations, () => AddPackageReference(projectManager, package.Id, package.Version, ignoreDependencies), logger);
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

        public virtual void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies, ILogger logger) {
            InitializeLogger(logger, projectManager);

            IPackage package = null;
            bool existsInProject = false;

            // Try the project repository
            if (projectManager != null) {
                package = projectManager.LocalRepository.FindPackage(packageId);
                existsInProject = package != null;
            }

            // Fallback to the solution repository (it might be a solution only package)
            package = package ?? LocalRepository.FindPackage(packageId);

            if (package == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.UnknownPackage, packageId));
            }

            IPackage newPackage = SourceRepository.FindPackage(packageId, version: version);

            if (newPackage != null && package.Version != newPackage.Version) {                
                if (existsInProject) {
                    // If the package exists in the project then install it solution level.
                    InstallPackage(newPackage, !updateDependencies);

                    UpdatePackageReference(projectManager, packageId, version, updateDependencies);
                }
                else {
                    // We might be updating a solution only package
                    UpdatePackage(package, newPackage, updateDependencies);
                }

                // Add package to recent repository
                AddPackageToRecentRepository(newPackage);
            }
            else {
                Logger.Log(MessageLevel.Info, VsResources.NoUpdatesAvailable, packageId);
            }
        }

        public void UpdatePackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, bool updateDependencies, ILogger logger) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }

            if (operations == null) {
                throw new ArgumentNullException("operations");
            }

            ExecuteOperatonsWithPackage(projectManager, package, operations, () => UpdatePackageReference(projectManager, package.Id, package.Version, updateDependencies), logger);
        }

        protected override void ExecuteUninstall(IPackage package) {
            // Check if the package is in use before removing it
            if (!_sharedRepository.IsReferenced(package.Id, package.Version)) {
                base.ExecuteUninstall(package);
            }
        }

        private void UpdatePackageReference(IProjectManager projectManager, string packageId, Version version, bool updateDependencies) {
            RunProjectActionWithRemoveEvent(projectManager, () => projectManager.UpdatePackageReference(packageId, version, updateDependencies));
        }

        private void AddPackageReference(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies) {
            RunProjectActionWithRemoveEvent(projectManager, () => projectManager.AddPackageReference(packageId, version, ignoreDependencies));
        }

        private void ExecuteOperatonsWithPackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, Action action, ILogger logger) {
            InitializeLogger(logger, projectManager);

            foreach (var operation in operations) {
                Execute(operation);
            }

            action();

            // Add package to recent repository
            AddPackageToRecentRepository(package);
        }

        private void RunProjectActionWithRemoveEvent(IProjectManager projectManager, Action action) {
            if (projectManager == null) {
                return;
            }

            EventHandler<PackageOperationEventArgs> removeHandler = (sender, e) => {
                // Remove any packages that would be removed as a result of updating a dependency or the package itself
                // We can execute the uninstall directly since we don't need to resolve dependencies again
                ExecuteUninstall(e.Package);
            };

            // Add the handlers
            projectManager.PackageReferenceRemoved += removeHandler;

            try {
                action();
            }
            finally {
                // Remove the handlers
                projectManager.PackageReferenceRemoved -= removeHandler;
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

        private void AddPackageToRecentRepository(IPackage package) {
            // add the installed package to the recent repository
            if (_recentPackagesRepository != null) {
                _recentPackagesRepository.AddPackage(new RecentPackage(package, SourceRepository.Source));
            }
        }
    }
}

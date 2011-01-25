using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    public class VsPackageManager : PackageManager, IVsPackageManager {
        private readonly ISharedPackageRepository _sharedRepository;
        private readonly IDictionary<string, IProjectManager> _projects;
        private readonly IPackageRepository _recentPackagesRepository;

        public VsPackageManager(ISolutionManager solutionManager,
                                IPackageRepository sourceRepository,
                                IFileSystem fileSystem,
                                ISharedPackageRepository sharedRepository,
                                IPackageRepository recentPackagesRepository) :
            base(sourceRepository, new DefaultPackagePathResolver(fileSystem), fileSystem, sharedRepository) {

            _sharedRepository = sharedRepository;
            _projects = solutionManager.GetProjects().ToDictionary(p => p.UniqueName, CreateProjectManager);
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
            _projects.TryGetValue(project.UniqueName, out projectManager);
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

            bool appliesToProject;
            IPackage package = FindLocalPackage(projectManager, packageId, out appliesToProject);

            if (appliesToProject) {
                projectManager.RemovePackageReference(packageId, forceRemove, removeDependencies);
            }

            UninstallPackage(package, forceRemove, removeDependencies);
        }

        public void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies) {
            UpdatePackage(projectManager, packageId, version, updateDependencies, NullLogger.Instance);
        }

        public virtual void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies, ILogger logger) {
            InitializeLogger(logger, projectManager);

            bool appliesToProject;
            IPackage package = FindLocalPackage(projectManager, packageId, out appliesToProject);

            // Find the package we're going to update to
            IPackage newPackage = SourceRepository.FindPackage(packageId, version);

            if (newPackage != null && package.Version != newPackage.Version) {
                if (appliesToProject) {
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

        private IPackage FindLocalPackage(IProjectManager projectManager, string packageId, out bool appliesToProject) {
            IPackage package = null;
            bool existsInProject = false;
            appliesToProject = false;

            if (projectManager != null) {
                // Try the project repository first
                package = projectManager.LocalRepository.FindPackage(packageId);

                existsInProject = package != null;
            }

            // Fallback to the solution repository (it might be a solution only package)
            package = package ?? LocalRepository.FindPackage(packageId);

            // Can't find the package in the solution or in the project then fail
            if (package == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.UnknownPackage, packageId));
            }


            // Check to see if this package applies to a project based on 3 criteria:
            // 1. If the package exists in the current project (we assume it was installed into the project in the first place)
            // 3. The package has project content (i.e. content that can be applied to a project lib or content files)
            // 2. The package is referenced by any other project

            // This logic will probably fail in one edge case. If there is a meta package that applies to a project
            // that ended up not being installed in any of the projects and it only exists at solution level.
            // If this happens, then we think that the following operation applies to the solution instead of showing an error.
            // To solve that edge case we'd have to walk the graph to find out what the package applies to.
            appliesToProject = existsInProject ||
                               package.HasProjectContent() ||
                               _sharedRepository.IsReferenced(package.Id, package.Version);

            if (appliesToProject) {
                if (!existsInProject) {
                    if (_sharedRepository.IsReferenced(package.Id, package.Version)) {
                        // If the package doesn't exist in the project and is referenced by other projects
                        // then fail.
                        if (projectManager != null) {
                            throw new InvalidOperationException(
                                    String.Format(CultureInfo.CurrentCulture,
                                    VsResources.UnknownPackageInProject,
                                    packageId,
                                    projectManager.Project.ProjectName));
                        }
                    }
                    else {
                        // The operation applies to solution level since it's not installed in the current project
                        // but it is installed in some other project
                        appliesToProject = false;
                    }
                }
            }

            // Can't have a project level operation if no project was specified
            if (appliesToProject && projectManager == null) {
                throw new InvalidOperationException(VsResources.ProjectNotSpecified);
            }

            return package;
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

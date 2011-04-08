using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    public class VsPackageManager : PackageManager, IVsPackageManager {
        private readonly ISharedPackageRepository _sharedRepository;
        private readonly IDictionary<string, IProjectManager> _projects;
        private readonly ISolutionManager _solutionManager;
        private readonly IPackageRepository _recentPackagesRepository;

        public VsPackageManager(ISolutionManager solutionManager, IPackageRepository sourceRepository, IFileSystem fileSystem, ISharedPackageRepository sharedRepository, IPackageRepository recentPackagesRepository) :
            base(sourceRepository, new DefaultPackagePathResolver(fileSystem), fileSystem, sharedRepository) {

            _solutionManager = solutionManager;
            _sharedRepository = sharedRepository;
            _recentPackagesRepository = recentPackagesRepository;

            _projects = new Dictionary<string, IProjectManager>(StringComparer.OrdinalIgnoreCase);
        }

        internal void EnsureCached(Project project) {
            if (_projects.ContainsKey(project.UniqueName)) {
                return;
            }

            _projects[project.UniqueName] = CreateProjectManager(project);
        }

        public virtual IProjectManager GetProjectManager(Project project) {
            EnsureCached(project);
            IProjectManager projectManager;
            bool projectExists = _projects.TryGetValue(project.UniqueName, out projectManager);
            Debug.Assert(projectExists, "Unknown project");
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

            IPackage package = PackageHelper.ResolvePackage(SourceRepository, LocalRepository, packageId, version);

            RunSolutionAction(() => {
                InstallPackage(package, ignoreDependencies);
                AddPackageReference(projectManager, packageId, version, ignoreDependencies);
            });

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
            IPackage package = FindLocalPackage(projectManager,
                                                packageId,
                                                version,
                                                CreateAmbiguousUninstallException,
                                                out appliesToProject);

            if (appliesToProject) {
                RemovePackageReference(projectManager, packageId, forceRemove, removeDependencies);
            }
            else {
                UninstallPackage(package, forceRemove, removeDependencies);
            }
        }

        public void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies) {
            UpdatePackage(projectManager, packageId, version, updateDependencies, NullLogger.Instance);
        }

        public virtual void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies, ILogger logger) {
            InitializeLogger(logger, projectManager);

            bool appliesToProject;
            IPackage package = FindLocalPackage(projectManager,
                                                packageId,
                                                null /* version */,
                                                CreateAmbiguousUpdateException,
                                                out appliesToProject);

            // Find the package we're going to update to
            IPackage newPackage = SourceRepository.FindPackage(packageId, version);

            if (newPackage != null && package.Version != newPackage.Version) {
                if (appliesToProject) {
                    RunSolutionAction(() => {
                        InstallPackage(newPackage, !updateDependencies);
                        UpdatePackageReference(projectManager, packageId, version, updateDependencies);
                    });

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

        private IPackage FindLocalPackage(IProjectManager projectManager,
                                          string packageId,
                                          Version version,
                                          Func<IProjectManager, IList<IPackage>, Exception> getAmbiguousMatchException,
                                          out bool appliesToProject) {
            IPackage package = null;
            bool existsInProject = false;
            appliesToProject = false;

            if (projectManager != null) {
                // Try the project repository first
                package = projectManager.LocalRepository.FindPackage(packageId, version);

                existsInProject = package != null;
            }

            // Fallback to the solution repository (it might be a solution only package)
            if (package == null) {
                if (version != null) {
                    // Get the exact package
                    package = LocalRepository.FindPackage(packageId, version);
                }
                else {
                    // Get all packages by this name to see if we find an ambiguous match
                    var packages = LocalRepository.FindPackagesById(packageId).ToList();
                    if (packages.Count > 1) {
                        throw getAmbiguousMatchException(projectManager, packages);
                    }

                    // Pick the only one of default if none match
                    package = packages.SingleOrDefault();
                }
            }

            // Can't find the package in the solution or in the project then fail
            if (package == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.UnknownPackage, packageId));
            }

            appliesToProject = IsProjectLevel(package);

            if (appliesToProject) {
                if (!existsInProject) {
                    if (_sharedRepository.IsReferenced(package.Id, package.Version)) {
                        // If the package doesn't exist in the project and is referenced by other projects
                        // then fail.
                        if (projectManager != null) {
                            throw new InvalidOperationException(
                                    String.Format(CultureInfo.CurrentCulture,
                                    VsResources.UnknownPackageInProject,
                                    package.GetFullName(),
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

        /// <summary>
        /// Check to see if this package applies to a project based on 2 criteria:
        /// 1. The package has project content (i.e. content that can be applied to a project lib or content files)
        /// 2. The package is referenced by any other project
        /// 
        /// This logic will probably fail in one edge case. If there is a meta package that applies to a project
        /// that ended up not being installed in any of the projects and it only exists at solution level.
        /// If this happens, then we think that the following operation applies to the solution instead of showing an error.
        /// To solve that edge case we'd have to walk the graph to find out what the package applies to.
        /// </summary>
        private bool IsProjectLevel(IPackage package) {
            return package.HasProjectContent() || _sharedRepository.IsReferenced(package.Id, package.Version);
        }

        private Exception CreateAmbiguousUpdateException(IProjectManager projectManager, IList<IPackage> packages) {
            if (projectManager != null && packages.Any(IsProjectLevel)) {
                return new InvalidOperationException(
                                    String.Format(CultureInfo.CurrentCulture,
                                    VsResources.UnknownPackageInProject,
                                    packages[0].Id,
                                    projectManager.Project.ProjectName));
            }

            return new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.AmbiguousUpdate,
                    packages[0].Id));
        }

        private Exception CreateAmbiguousUninstallException(IProjectManager projectManager, IList<IPackage> packages) {
            if (projectManager != null && packages.Any(IsProjectLevel)) {
                return new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.AmbiguousProjectLevelUninstal,
                    packages[0].Id,
                    projectManager.Project.ProjectName));
            }

            return new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.AmbiguousUninstall,
                    packages[0].Id));
        }

        private void RemovePackageReference(IProjectManager projectManager, string packageId, bool forceRemove, bool removeDependencies) {
            RunProjectAction(projectManager, () => projectManager.RemovePackageReference(packageId, forceRemove, removeDependencies));
        }

        private void UpdatePackageReference(IProjectManager projectManager, string packageId, Version version, bool updateDependencies) {
            RunProjectAction(projectManager, () => projectManager.UpdatePackageReference(packageId, version, updateDependencies));
        }

        private void AddPackageReference(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies) {
            RunProjectAction(projectManager, () => projectManager.AddPackageReference(packageId, version, ignoreDependencies));
        }

        private void ExecuteOperatonsWithPackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, Action action, ILogger logger) {
            InitializeLogger(logger, projectManager);

            RunSolutionAction(() => {
                if (operations.Any()) {
                    foreach (var operation in operations) {
                        Execute(operation);
                    }
                }
                else if (LocalRepository.Exists(package)) {
                    Logger.Log(MessageLevel.Info, VsResources.Log_PackageAlreadyInstalled, package.GetFullName());
                }

                action();
            });

            // Add package to recent repository
            AddPackageToRecentRepository(package);
        }

        private Project GetProject(IProjectManager projectManager) {
            // We only support project systems that implement IVsProjectSystem
            var vsProjectSystem = projectManager.Project as IVsProjectSystem;
            if (vsProjectSystem == null) {
                return null;
            }

            // Find the project by it's unique name
            return _solutionManager.GetProject(vsProjectSystem.UniqueName);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "If we failed to add binding redirects we don't want it to stop the install/update.")]
        private void AddBindingRedirects(IProjectManager projectManager) {
            // Find the project by it's unique name
            Project project = GetProject(projectManager);

            // If we can't find the project or it doesn't support binding redirects then don't add any redirects
            if (project == null || !project.SupportsBindingRedirects()) {
                return;
            }

            try {
                RuntimeHelpers.AddBindingRedirects(_solutionManager, project);
            }
            catch (Exception e) {
                // If there was an error adding binding redirects then print a warning and continue
                Logger.Log(MessageLevel.Warning, String.Format(CultureInfo.CurrentCulture, VsResources.Warning_FailedToAddBindingRedirects, projectManager.Project.ProjectName, e.Message));
            }
        }

        private void AddPackageToRecentRepository(IPackage package) {
            // add the installed package to the recent repository
            if (_recentPackagesRepository != null) {
                _recentPackagesRepository.AddPackage(package);
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
        /// <summary>
        /// Runs the specified action and rolls back any installed packages if on failure.
        /// </summary>
        private void RunSolutionAction(Action action) {
            var packagesAdded = new List<IPackage>();

            EventHandler<PackageOperationEventArgs> installedHandler = (sender, e) => {
                // Record packages that we successfully installed
                packagesAdded.Add(e.Package);
            };

            PackageInstalled += installedHandler;

            try {
                // Execute the action
                action();
            }
            catch {
                if (packagesAdded.Any()) {
                    // Only print the rollback warning if we have something to rollback
                    Logger.Log(MessageLevel.Warning, VsResources.Warning_RollingBack);
                }

                // Don't log anything during the rollback
                Logger = null;

                // Rollback the install if it fails
                Uninstall(packagesAdded);
                throw;
            }
            finally {
                // Remove the event handler
                PackageInstalled -= installedHandler;
            }
        }

        /// <summary>
        /// Runs action on the project manager and rollsback any package installs if it fails.
        /// </summary>
        private void RunProjectAction(IProjectManager projectManager, Action action) {
            if (projectManager == null) {
                return;
            }

            // Keep track of what was added and removed
            var packagesAdded = new Stack<IPackage>();
            var packagesRemoved = new List<IPackage>();

            EventHandler<PackageOperationEventArgs> removeHandler = (sender, e) => {
                packagesRemoved.Add(e.Package);
            };

            EventHandler<PackageOperationEventArgs> addedHandler = (sender, e) => {
                packagesAdded.Push(e.Package);
            };

            // Try to get the project for this project manager
            Project project = GetProject(projectManager);

            IVsProjectBuildSystem build = null;

            if (project != null) {
                build = project.ToVsProjectBuildSystem();
            }

            // Add the handlers
            projectManager.PackageReferenceRemoved += removeHandler;
            projectManager.PackageReferenceAdding += addedHandler;

            try {
                if (build != null) {
                    // Start a batch edit so there is no background compilation until we're done
                    // processing project actions
                    build.StartBatchEdit();
                }

                action();

                // Only add binding redirects if install was successful
                AddBindingRedirects(projectManager);
            }
            catch {
                // We need to Remove the handlers here since we're going to attempt
                // a rollback and we don't want modify the collections while rolling back.
                projectManager.PackageReferenceRemoved -= removeHandler;
                projectManager.PackageReferenceAdded -= addedHandler;

                // When things fail attempt a rollback
                RollbackProjectActions(projectManager, packagesAdded, packagesRemoved);

                // Clear removed packages so we don't try to remove them again (small optimization)
                packagesRemoved.Clear();
                throw;
            }
            finally {
                if (build != null) {
                    // End the batch edit when we are done.
                    build.EndBatchEdit();
                }

                // Remove the handlers
                projectManager.PackageReferenceRemoved -= removeHandler;
                projectManager.PackageReferenceAdded -= addedHandler;

                // Remove any packages that would be removed as a result of updating a dependency or the package itself
                // We can execute the uninstall directly since we don't need to resolve dependencies again.
                Uninstall(packagesRemoved);
            }
        }

        private static void RollbackProjectActions(IProjectManager projectManager, IEnumerable<IPackage> packagesAdded, IEnumerable<IPackage> packagesRemoved) {
            // Disable logging when rolling back project operations
            projectManager.Logger = null;

            foreach (var package in packagesAdded) {
                // Remove each package that was added
                projectManager.RemovePackageReference(package, forceRemove: false, removeDependencies: false);
            }

            foreach (var package in packagesRemoved) {
                // Add each package that was removed
                projectManager.AddPackageReference(package, ignoreDependencies: true);
            }
        }

        private void Uninstall(IEnumerable<IPackage> packages) {
            foreach (var package in packages) {
                ExecuteUninstall(package);
            }
        }
    }
}

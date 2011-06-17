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

        internal bool AddToRecent { get; set; }

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

            // The source repository of the project is an aggregate since it might need to look for all
            // available packages to perform updates on dependent packages
            var sourceRepository = new AggregateRepository(new[] { _sharedRepository, SourceRepository });

            var projectManager = new ProjectManager(sourceRepository, PathResolver, projectSystem, repository);

            // The package reference repository also provides constraints for packages (via the allowedVersions attribute)
            projectManager.ConstraintProvider = repository;
            return projectManager;
        }

        public void InstallPackage(
            IEnumerable<Project> projects,
            IPackage package,
            IEnumerable<PackageOperation> operations,
            bool ignoreDependencies,
            ILogger logger,
            IPackageOperationEventListener packageOperationEventListener) {

            if (package == null) {
                throw new ArgumentNullException("package");
            }

            if (operations == null) {
                throw new ArgumentNullException("operations");
            }

            if (projects == null) {
                throw new ArgumentNullException("projectManagers");
            }

            ExecuteOperationsWithPackage(
                projects,
                package,
                operations,
                projectManager => AddPackageReference(projectManager, package.Id, package.Version, ignoreDependencies),
                logger,
                packageOperationEventListener);
        }

        public void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies) {
            InstallPackage(projectManager, packageId, version, ignoreDependencies, NullLogger.Instance);
        }

        public virtual void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies, ILogger logger) {
            InitializeLogger(logger, projectManager);

            IPackage package = PackageHelper.ResolvePackage(SourceRepository, LocalRepository, packageId, version);

            RunSolutionAction(() => {
                InstallPackage(package, ignoreDependencies);
                AddPackageReference(projectManager, package.Id, package.Version, ignoreDependencies);
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

            ExecuteOperationsWithPackage(
                projectManager,
                package,
                operations,
                () => AddPackageReference(projectManager, package.Id, package.Version, ignoreDependencies),
                logger);
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

        public void UpdatePackage(
            IEnumerable<Project> projects,
            IPackage package,
            IEnumerable<PackageOperation> operations,
            bool updateDependencies,
            ILogger logger,
            IPackageOperationEventListener packageOperationEventListener) {

            if (operations == null) {
                throw new ArgumentNullException("operations");
            }

            if (projects == null) {
                throw new ArgumentNullException("projects");
            }

            ExecuteOperationsWithPackage(
                projects,
                package,
                operations,
                projectManager => UpdatePackageReference(projectManager, package.Id, package.Version, updateDependencies),
                logger,
                packageOperationEventListener);
        }

        public void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies) {
            UpdatePackage(projectManager, packageId, version, updateDependencies, NullLogger.Instance);
        }

        public virtual void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies, ILogger logger) {
            UpdatePackage(projectManager,
                          packageId,
                          () => UpdatePackageReference(projectManager, packageId, version, updateDependencies),
                          () => SourceRepository.FindPackage(packageId, version),
                          updateDependencies,
                          logger);
        }

        private void UpdatePackage(IProjectManager projectManager, string packageId, Action projectAction, Func<IPackage> resolvePackage, bool updateDependencies, ILogger logger) {
            InitializeLogger(logger, projectManager);

            bool appliesToProject;
            IPackage package = FindLocalPackageForUpdate(projectManager, packageId, out appliesToProject);

            // Find the package we're going to update to
            IPackage newPackage = resolvePackage();

            if (newPackage != null && package.Version != newPackage.Version) {
                if (appliesToProject) {
                    RunSolutionAction(projectAction);
                }
                else {
                    // We might be updating a solution only package
                    UpdatePackage(newPackage, updateDependencies);
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

            ExecuteOperationsWithPackage(projectManager, package, operations, () => UpdatePackageReference(projectManager, package.Id, package.Version, updateDependencies), logger);
        }

        public void UpdatePackage(string packageId, IVersionSpec versionSpec, bool updateDependencies, ILogger logger) {
            UpdatePackage(packageId,
                          projectManager => UpdatePackageReference(projectManager, packageId, versionSpec, updateDependencies),
                          () => SourceRepository.FindPackage(packageId, versionSpec),
                          updateDependencies,
                          logger,
                          NullPackageOperationEventListener.Instance);
        }

        public void UpdatePackage(string packageId, Version version, bool updateDependencies, ILogger logger, IPackageOperationEventListener eventListener) {
            UpdatePackage(packageId,
                          projectManager => UpdatePackageReference(projectManager, packageId, version, updateDependencies),
                          () => SourceRepository.FindPackage(packageId, version),
                          updateDependencies,
                          logger,
                          eventListener);
        }

        public void UpdatePackage(string packageId, Version version, bool updateDependencies, ILogger logger) {
            UpdatePackage(packageId,
                          projectManager => UpdatePackageReference(projectManager, packageId, version, updateDependencies),
                          () => SourceRepository.FindPackage(packageId, version),
                          updateDependencies,
                          logger,
                          NullPackageOperationEventListener.Instance);
        }

        public void UpdatePackages(bool updateDependencies, ILogger logger) {
            UpdatePackages(updateDependencies, safeUpdate: false, logger: logger);
        }

        public void UpdatePackages(IProjectManager projectManager, bool updateDependencies, ILogger logger) {
            UpdatePackages(projectManager, updateDependencies, safeUpdate: false, logger: logger);
        }

        public void SafeUpdatePackages(IProjectManager projectManager, bool updateDependencies, ILogger logger) {
            UpdatePackages(projectManager, updateDependencies, safeUpdate: true, logger: logger);
        }

        public void SafeUpdatePackage(string packageId, bool updateDependencies, ILogger logger) {
            UpdatePackage(packageId,
                          projectManager => UpdatePackageReference(projectManager, packageId, GetSafeRange(projectManager, packageId), updateDependencies),
                          () => SourceRepository.FindPackage(packageId, GetSafeRange(packageId)),
                          updateDependencies,
                          logger,
                          NullPackageOperationEventListener.Instance);
        }

        public void SafeUpdatePackage(IProjectManager projectManager, string packageId, bool updateDependencies, ILogger logger) {
            UpdatePackage(projectManager,
                          packageId,
                          () => UpdatePackageReference(projectManager, packageId, GetSafeRange(projectManager, packageId), updateDependencies),
                          () => SourceRepository.FindPackage(packageId, GetSafeRange(packageId)),
                          updateDependencies,
                          logger);
        }

        public void SafeUpdatePackages(bool updateDependencies, ILogger logger) {
            UpdatePackages(updateDependencies, safeUpdate: true, logger: logger);
        }

        protected override void ExecuteUninstall(IPackage package) {
            // Check if the package is in use before removing it
            if (!_sharedRepository.IsReferenced(package.Id, package.Version)) {
                base.ExecuteUninstall(package);
            }
        }

        private IPackage FindLocalPackageForUpdate(IProjectManager projectManager, string packageId, out bool appliesToProject) {
            return FindLocalPackage(projectManager, packageId, null /* version */, CreateAmbiguousUpdateException, out appliesToProject);
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
                            if (version == null) {
                                throw new InvalidOperationException(
                                        String.Format(CultureInfo.CurrentCulture,
                                        VsResources.UnknownPackageInProject,
                                        package.Id,
                                        projectManager.Project.ProjectName));
                            }

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

        private IPackage FindLocalPackage(string packageId, out bool appliesToProject) {
            // It doesn't matter if there are multiple versions of the package installed at solution level, 
            // we just want to know that one exists.
            var packages = LocalRepository.FindPackagesById(packageId).ToList();

            // Can't find the package in the solution.
            if (!packages.Any()) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.UnknownPackage, packageId));
            }

            IPackage package = packages.FirstOrDefault();
            appliesToProject = IsProjectLevel(package);

            if (!appliesToProject) {
                if (packages.Count > 1) {
                    throw CreateAmbiguousUpdateException(projectManager: null, packages: packages);
                }
            }
            else if (!_sharedRepository.IsReferenced(package.Id, package.Version)) {
                // If this package applies to a project but isn't installed in any project then 
                // it's probably a borked install.
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.PackageNotInstalledInAnyProject, packageId));
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
        public bool IsProjectLevel(IPackage package) {
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

        private void UpdatePackageReference(IProjectManager projectManager, string packageId, IVersionSpec versionSpec, bool updateDependencies) {
            RunProjectAction(projectManager, () => projectManager.UpdatePackageReference(packageId, versionSpec, updateDependencies));
        }

        private void AddPackageReference(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies) {
            RunProjectAction(projectManager, () => projectManager.AddPackageReference(packageId, version, ignoreDependencies));
        }

        private void ExecuteOperationsWithPackage(IEnumerable<Project> projects, IPackage package, IEnumerable<PackageOperation> operations, Action<IProjectManager> projectAction, ILogger logger, IPackageOperationEventListener packageOperationEventListener) {
            if (packageOperationEventListener == null) {
                packageOperationEventListener = NullPackageOperationEventListener.Instance;
            }

            ExecuteOperationsWithPackage(
                null,
                package,
                operations,
                () => {
                    bool success = false;

                    foreach (var project in projects) {
                        try {
                            packageOperationEventListener.OnBeforeAddPackageReference(project);

                            IProjectManager projectManager = GetProjectManager(project);
                            InitializeLogger(logger, projectManager);

                            projectAction(projectManager);
                            success |= true;
                        }
                        catch (Exception ex) {
                            packageOperationEventListener.OnAddPackageReferenceError(project, ex);
                        }
                        finally {
                            packageOperationEventListener.OnAfterAddPackageReference(project);
                        }
                    }

                    // Throw an exception only if all the update failed for all projects
                    // so we rollback any solution level operations that might have happened
                    if (projects.Any() && !success) {
                        throw new InvalidOperationException(VsResources.OperationFailed);
                    }

                },
                logger);
        }

        private void ExecuteOperationsWithPackage(IProjectManager projectManager, IPackage package, IEnumerable<PackageOperation> operations, Action action, ILogger logger) {
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
            if (!AddToRecent) {
                return;
            }

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

            EventHandler<PackageOperationEventArgs> installHandler = (sender, e) => {
                // Record packages that we are installing so that if one fails, we can rollback
                packagesAdded.Add(e.Package);
            };

            PackageInstalling += installHandler;

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
                PackageInstalling -= installHandler;
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

            EventHandler<PackageOperationEventArgs> addingHandler = (sender, e) => {
                packagesAdded.Push(e.Package);

                // If this package doesn't exist at solution level (it might not because of leveling)
                // then we need to install it.
                if (!LocalRepository.Exists(e.Package)) {
                    ExecuteInstall(e.Package);
                }
            };


            // Try to get the project for this project manager
            Project project = GetProject(projectManager);

            IVsProjectBuildSystem build = null;

            if (project != null) {
                build = project.ToVsProjectBuildSystem();
            }

            // Add the handlers
            projectManager.PackageReferenceRemoved += removeHandler;
            projectManager.PackageReferenceAdding += addingHandler;

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
                projectManager.PackageReferenceAdded -= addingHandler;

                // When things fail attempt a rollback
                RollbackProjectActions(projectManager, packagesAdded, packagesRemoved);

                // Rollback solution packages
                Uninstall(packagesAdded);

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
                projectManager.PackageReferenceAdding -= addingHandler;

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

        private void UpdatePackage(string packageId, Action<IProjectManager> projectAction, Func<IPackage> resolvePackage, bool updateDependencies, ILogger logger, IPackageOperationEventListener eventListener) {
            bool appliesToProject;
            IPackage package = FindLocalPackage(packageId, out appliesToProject);

            if (appliesToProject) {
                eventListener = eventListener ?? NullPackageOperationEventListener.Instance;

                IPackage newPackage = null;

                foreach (var project in _solutionManager.GetProjects()) {
                    IProjectManager projectManager = GetProjectManager(project);
                    InitializeLogger(logger, projectManager);

                    if (projectManager.LocalRepository.Exists(packageId)) {
                        eventListener.OnBeforeAddPackageReference(project);
                        try {
                            RunSolutionAction(() => projectAction(projectManager));
                            
                            if (newPackage == null) {
                                // after the update, get the new version to add to the recent package repository
                                newPackage = projectManager.LocalRepository.FindPackage(packageId);
                            }
                        }
                        catch (Exception e) {
                            logger.Log(MessageLevel.Error, e.Message);
                            eventListener.OnAddPackageReferenceError(project, e);
                        }
                        finally {
                            eventListener.OnAfterAddPackageReference(project);
                        }
                    }
                }

                if (newPackage != null) {
                    AddPackageToRecentRepository(newPackage);
                }
            }
            else {
                // Find the package we're going to update to
                IPackage newPackage = resolvePackage();

                if (newPackage != null && package.Version != newPackage.Version) {
                    // We might be updating a solution only package
                    UpdatePackage(newPackage, updateDependencies);

                    // Add package to recent repository
                    AddPackageToRecentRepository(newPackage);
                }
                else {
                    Logger.Log(MessageLevel.Info, VsResources.NoUpdatesAvailable, packageId);
                }
            }
        }

        private void UpdatePackages(IProjectManager projectManager, bool updateDependencies, bool safeUpdate, ILogger logger) {
            UpdatePackages(projectManager.LocalRepository, package => {
                if (safeUpdate) {
                    SafeUpdatePackage(projectManager, package.Id, updateDependencies, logger: logger);
                }
                else {
                    UpdatePackage(projectManager, package.Id, version: null, updateDependencies: updateDependencies, logger: logger);
                }
            }, logger);
        }

        private void UpdatePackages(bool updateDependencies, bool safeUpdate, ILogger logger) {
            UpdatePackages(LocalRepository, package => {
                if (safeUpdate) {
                    SafeUpdatePackage(package.Id, updateDependencies, logger: logger);
                }
                else {
                    UpdatePackage(package.Id, version: null, updateDependencies: updateDependencies, logger: logger);
                }
            }, logger);
        }

        private void UpdatePackages(IPackageRepository localRepository, Action<IPackage> updateAction, ILogger logger) {
            var packageSorter = new PackageSorter();
            // Get the packages in reverse dependency order then run update on each one i.e. if A -> B run Update(A) then Update(B)
            var packages = packageSorter.GetPackagesByDependencyOrder(localRepository).Reverse();
            foreach (var package in packages) {
                // While updating we might remove packages that were initially in the list. e.g.
                // A 1.0 -> B 2.0, A 2.0 -> [], since updating to A 2.0 removes B, we end up skipping it.
                if (localRepository.Exists(package.Id)) {
                    try {
                        updateAction(package);
                    }
                    catch (Exception e) {
                        logger.Log(MessageLevel.Error, e.Message);
                    }
                }
            }
        }

        private IVersionSpec GetSafeRange(string packageId) {
            bool appliesToProject;
            IPackage package = FindLocalPackage(packageId, out appliesToProject);
            return VersionUtility.GetSafeRange(package.Version);
        }

        private IVersionSpec GetSafeRange(IProjectManager projectManager, string packageId) {
            bool appliesToProject;
            IPackage package = FindLocalPackageForUpdate(projectManager, packageId, out appliesToProject);
            return VersionUtility.GetSafeRange(package.Version);
        }
    }
}

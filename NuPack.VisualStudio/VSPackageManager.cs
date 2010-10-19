using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;

namespace NuPack.VisualStudio {
    public class VSPackageManager : PackageManager {
        private const string SolutionRepositoryDirectory = "packages";
        private readonly Dictionary<Project, ProjectManager> _projectManagers = null;
        private static SolutionEvents _solutionEvents;
        private static IFileSystem _solutionFileSystem;
        private static IPackageRepository _solutionRepository;

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dte", Justification = "dte is the vs automation object")]
        public VSPackageManager(DTE dte) :
            this(dte, VSPackageSourceProvider.GetRepository(dte)) {
        }

        /// <summary>
        /// This overload is called from Powershell script
        /// </summary>
        /// <param name="dte"></param>
        public VSPackageManager(object dte) :
            this((DTE)dte) {
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dte", Justification = "dte is the vs automation object")]
        public VSPackageManager(DTE dte, IPackageRepository sourceRepository) :
            base(sourceRepository: sourceRepository,
                pathResolver: new DefaultPackagePathResolver(GetFileSystem(dte)),
                fileSystem: GetFileSystem(dte),
                localRepository: GetSolutionRepository(dte)) {

            _projectManagers = SolutionManager.Current.GetProjects().ToDictionary(project => project,
                                                                                  CreateProjectManager);
        }

        private IEnumerable<ProjectManager> ProjectManagers {
            get {
                return _projectManagers.Values;
            }
        }

        public ProjectManager GetProjectManager(Project project) {
            ProjectManager projectManager;
            _projectManagers.TryGetValue(project, out projectManager);
            return projectManager;
        }

        public void InstallPackage(Project project, string packageId, Version version, bool ignoreDependencies, ILogger logger) {
            IPackage package = SourceRepository.FindPackage(packageId, version);

            if (package == null) {
                // TODO: Error handling
                return;
            }

            var resolver = new VSInstallDependencyResolver(LocalRepository, SourceRepository, logger, ignoreDependencies);
            resolver.Resolve(package);

            // Execute the solution only operations
            ExecuteSolutionOperations(resolver.SolutionOperations, logger);

            // If we're going an operation on all projects then report exceptions as warnings
            bool exceptionsAsWarnings = project == null;

            foreach (var projectManager in GetTargetProjects(project)) {
                IPackageOperationResolver projectResolver = GetResolver(projectManager, ignoreDependencies);
                try {
                    // Execute project level operations
                    ExecuteProjectOperations(resolver.ProjectOperations, projectManager, logger, new HashSet<IPackage>(), projectResolver);
                }
                catch (Exception e) {
                    if (exceptionsAsWarnings) {
                        logger.Log(MessageLevel.Warning, e.Message);
                    }
                    else {
                        throw;
                    }
                }
            }
        }

        private IEnumerable<ProjectManager> GetTargetProjects(Project project) {
            if (project != null) {
                yield return GetProjectManager(project);
            }
            else {
                foreach (var projectManager in ProjectManagers) {
                    yield return projectManager;
                }
            }
        }

        private IPackageOperationResolver GetResolver(ProjectManager projectManager, bool ignoreDependencies) {
            return new ProjectInstallWalker(projectManager.LocalRepository,
                                            projectManager.SourceRepository,
                                            new DependentsWalker(projectManager.LocalRepository),
                                            NullLogger.Instance,
                                            ignoreDependencies);

        }

        public void UninstallPackage(Project project, string id, Version version, bool forceRemove, bool removeDependencies, ILogger logger) {
            IPackage package = LocalRepository.FindPackage(id, version);

            if (package == null) {
                // TODO: Error handling
                return;
            }

            var resolver = new VSUninstallDependencyResolver(LocalRepository,
                                                             new DependentsWalker(LocalRepository),
                                                             logger,
                                                             removeDependencies,
                                                             forceRemove);
            resolver.Resolve(package);

            // Execute the solution only operations
            ExecuteSolutionOperations(resolver.SolutionOperations, logger);

            bool exceptionsAsWarnings = project == null;

            // Execute project level operations
            foreach (var projectManager in GetTargetProjects(project)) {
                try {
                    ExecuteProjectOperations(resolver.ProjectOperations, projectManager, logger, null, null);
                }
                catch (Exception e) {
                    if (exceptionsAsWarnings) {
                        logger.Log(MessageLevel.Warning, e.Message);
                    }
                    else {
                        throw;
                    }
                }
            }

        }

        private void ExecuteProjectOperations(IEnumerable<PackageOperation> operations,
                                              ProjectManager projectManager,
                                              ILogger logger,
                                              HashSet<IPackage> processed,
                                              IPackageOperationResolver projectResolver) {
            try {
                // REVIEW: We shouldn't have to set loggers at this level
                FileSystem.Logger = logger;
                projectManager.Logger = logger;
                projectManager.Project.Logger = logger;

                foreach (var operation in operations) {
                    if (operation.Action == PackageAction.Uninstall) {
                        ExecuteProjectUninstall(projectManager, operation, logger);
                    }
                    else {
                        // Do nothing if this package has already been verified
                        if (!processed.Contains(operation.Package)) {
                            // Keep track of the list of verified packages so we don't walk the graph more than we need to
                            IEnumerable<PackageOperation> installOperations = projectResolver.ResolveOperations(operation.Package);
                            processed.Add(operation.Package);
                            ExecuteProjectOperations(installOperations, projectManager, logger, processed, projectResolver);
                        }
                        else {
                            ExecuteProjectInstall(projectManager, operation);
                        }
                    }
                }
            }
            finally {
                projectManager.Logger = null;
                projectManager.Project.Logger = null;
                FileSystem.Logger = null;
            }
        }

        private void ExecuteSolutionOperations(IEnumerable<PackageOperation> operations, ILogger logger) {
            try {
                FileSystem.Logger = logger;
                Logger = logger;

                // Execute solution only operations
                foreach (var operation in operations) {
                    Execute(operation);
                }
            }
            finally {
                Logger = null;
                FileSystem.Logger = null;
            }
        }

        private void ExecuteProjectInstall(ProjectManager projectManager, PackageOperation operation) {
            Execute(operation);

            projectManager.Execute(operation);

        }

        private void ExecuteProjectUninstall(ProjectManager projectManager, PackageOperation operation, ILogger logger) {
            try {
                // If the package doesn't exist
                if (!projectManager.LocalRepository.Exists(operation.Package)) {
                    Logger = logger;
                }

                projectManager.Execute(operation);

                if (!IsPackageReferenced(operation.Package)) {
                    Execute(operation);
                }
            }
            finally {
                if (Logger != null) {
                    Logger = null;
                }
            }
        }

        public void UpdatePackage(string packageId, Version version, bool updateDependencies, ILogger logger) {
            var projectManagers = GetProjectsWithPackage(packageId, version);
            InstallPackage(null, packageId, version, !updateDependencies, logger);
        }

        private bool IsPackageReferenced(IPackage package) {
            return GetProjectsWithPackage(package.Id, package.Version).Any();
        }

        private static IPackageRepository GetSolutionRepository(DTE dte) {
            EnsureSolutionRepository(dte);
            return _solutionRepository;
        }

        private static IFileSystem GetFileSystem(DTE dte) {
            EnsureSolutionRepository(dte);
            return _solutionFileSystem;
        }

        private static void EnsureSolutionRepository(DTE dte) {
            if (_solutionRepository == null) {
                EnsureSolutionEventBindings(dte);
                _solutionFileSystem = CreateFileSystem(dte);
                _solutionRepository = new LocalPackageRepository(new DefaultPackagePathResolver(_solutionFileSystem), _solutionFileSystem);
            }
        }

        private static void EnsureSolutionEventBindings(DTE dte) {
            if (_solutionEvents == null) {
                // Keep a reference to SolutionEvents so that it doesn't get GC'ed. Otherwise, we won't receive events.
                _solutionEvents = dte.Events.SolutionEvents;
                _solutionEvents.BeforeClosing += OnSolutionClosing;
            }
        }

        private static void OnSolutionClosing() {
            _solutionFileSystem = null;
            _solutionRepository = null;
        }

        private static IFileSystem CreateFileSystem(DTE dte) {
            // Get the component model service from dte                               
            var componentModel = dte.GetService<IComponentModel>(typeof(SComponentModel));

            Debug.Assert(componentModel != null, "Component model service is null");

            // Get the source control providers
            var providers = componentModel.GetExtensions<ISourceControlFileSystemProvider>();

            // Get the packages path
            string path = Path.Combine(Path.GetDirectoryName(dte.Solution.FullName), "packages");
            IFileSystem fileSystem = null;

            var sourceControl = (SourceControl2)dte.SourceControl;
            if (providers.Any() && sourceControl != null) {
                SourceControlBindings binding = null;
                try {
                    // Get the binding for this solution
                    binding = sourceControl.GetBindings(dte.Solution.FullName);
                }
                catch (NotImplementedException) {
                    // Some source control providers don't bother to implement this.
                    // TFS might be the only one using it
                }

                if (binding != null) {
                    fileSystem = providers.Select(provider => GetFileSystemFromProvider(provider, path, binding))
                                          .Where(fs => fs != null)
                                          .FirstOrDefault();
                }
            }

            return fileSystem ?? new FileBasedProjectSystem(path);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static IFileSystem GetFileSystemFromProvider(ISourceControlFileSystemProvider provider, string path, SourceControlBindings binding) {
            try {
                return provider.GetFileSystem(path, binding);
            }
            catch {
                // Ignore exceptions that can happen when some binaries are missing. e.g. TfsSourceControlFileSystemProvider
                // would throw a jitting error if TFS is not installed
            }

            return null;
        }
        private ProjectManager CreateProjectManager(Project project) {
            return new ProjectManager(LocalRepository, PathResolver, ProjectSystemFactory.CreateProjectSystem(project));
        }

        private IEnumerable<ProjectManager> GetProjectsWithPackage(string packageId, Version version) {
            return from projectManager in ProjectManagers
                   let package = projectManager.LocalRepository.FindPackage(packageId)
                   where package != null && (version == null || (version != null && package.Version.Equals(version)))
                   select projectManager;
        }
    }
}
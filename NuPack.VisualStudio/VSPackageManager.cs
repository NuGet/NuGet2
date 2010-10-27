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
    // TODO: Move this class into the NuGet.Core and change the name
    public class VsPackageManager : PackageManager, IVsPackageManager {
        private const string SolutionRepositoryDirectory = "packages";
        private readonly Dictionary<Project, IProjectManager> _projectManagers = null;

        private static SolutionEvents _solutionEvents;
        private static IFileSystem _solutionFileSystem;
        private static IPackageRepository _solutionRepository;

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dte", Justification = "dte is the vs automation object")]
        public VsPackageManager(DTE dte) :
            this(dte, VsPackageSourceProvider.GetRepository(dte)) {
        }

        /// <summary>
        /// This overload is called from Powershell script
        /// </summary>
        /// <param name="dte"></param>
        public VsPackageManager(object dte) :
            this((DTE)dte) {
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dte", Justification = "dte is the vs automation object")]
        public VsPackageManager(DTE dte, IPackageRepository sourceRepository) :
            this(SolutionManager.Current,
                sourceRepository,
                GetFileSystem(dte),
                GetSolutionRepository(dte)) {
        }

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

        private IProjectManager CreateProjectManager(Project project) {
            return new ProjectManager(LocalRepository, PathResolver, ProjectSystemFactory.CreateProjectSystem(project));
        }

        private IEnumerable<IProjectManager> GetProjectsWithPackage(string packageId, Version version) {
            return from projectManager in ProjectManagers
                   let package = projectManager.LocalRepository.FindPackage(packageId)
                   where package != null && (version == null || (version != null && package.Version.Equals(version)))
                   select projectManager;
        }
    }
}

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
            this(dte, VSPackageSourceProvider.GetRepository(dte))  {
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dte", Justification = "dte is the vs automation object")]
        public VSPackageManager(DTE dte, IPackageRepository sourceRepository) :
            base(sourceRepository: sourceRepository,
                pathResolver: new DefaultPackagePathResolver(GetFileSystem(dte)),
                fileSystem: GetFileSystem(dte),
                localRepository: GetSolutionRepository(dte)) {
            
            _projectManagers = SolutionManager.Current.GetProjects().ToDictionary(project => project, project => CreateProjectManager(project));
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

        public void UpdatePackage(string packageId, Version version, bool updateDependencies) {
            var projectManagers = GetProjectsWithPackage(packageId, version);
            if (projectManagers.Any()) {
                foreach (var projectManager in projectManagers) {
                    projectManager.UpdatePackageReference(packageId, version, updateDependencies);
                }
            }
            else {
                InstallPackage(packageId, version);
            }
        }

        public override void UninstallPackage(IPackage package, bool forceRemove = false, bool removeDependencies = false) {
            // Remove reference from projects that reference this package
            var projectManagers = GetProjectsWithPackage(package.Id, package.Version);
            if (projectManagers.Any()) {
                // We don't need to actually call uninstall since uninstalling it from all the projects
                // already has a side effect of removing it from the package manager
                foreach (ProjectManager projectManager in projectManagers) {
                    projectManager.RemovePackageReference(package.Id, forceRemove, removeDependencies);
                }
            }
            else {
                base.UninstallPackage(package, forceRemove, removeDependencies);
            }
        }

        internal void OnPackageReferenceRemoved(IPackage removedPackage, bool forceRemove = false, bool removeDependencies = false) {
            if (!IsPackageReferenced(removedPackage)) {
                // There are no packages that depend on this one so just uninstall it
                base.UninstallPackage(removedPackage.Id, removedPackage.Version, forceRemove, removeDependencies);
            }
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
            return new VSProjectManager(this, PathResolver, project);
        }

        private IEnumerable<ProjectManager> GetProjectsWithPackage(string packageId, Version version) {
            return from projectManager in ProjectManagers
                   let package = projectManager.LocalRepository.FindPackage(packageId)
                   where package != null && (version == null || (version != null && package.Version.Equals(version)))
                   select projectManager;
        }
    }
}
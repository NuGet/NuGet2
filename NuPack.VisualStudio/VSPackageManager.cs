namespace NuPack.VisualStudio {
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using EnvDTE;

    public class VSPackageManager : PackageManager {
        private static ConcurrentDictionary<string, VSPackageManager> _packageManagerCache = new ConcurrentDictionary<string, VSPackageManager>(StringComparer.OrdinalIgnoreCase);

        // List of prokect types
        // http://www.mztools.com/articles/2008/MZ2008017.aspx
        private static readonly string[] _supportedProjectTypes = new[] { VSConstants.WebSiteProjectKind, 
                                                                          VSConstants.CsharpProjectKind, 
                                                                          VSConstants.VbProjectKind };

        private Dictionary<Project, ProjectManager> _projectManagers = null;

        private readonly DTE _dte;
        private readonly SolutionEvents _solutionEvents;

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dte", Justification = "dte is the vs automation object")]
        public VSPackageManager(DTE dte, IPackageRepository sourceRepository, IFileSystem fileSystem)
            : base(sourceRepository, fileSystem) {
            if (dte == null) {
                throw new ArgumentNullException("dte");
            }

            _dte = dte;
            // Apparently you must hold on to the instance you get from here, or it may be garbage collected and you
            // lose your events.
            _solutionEvents = _dte.Events.SolutionEvents;

            _solutionEvents.ProjectAdded += OnProjectAdded;
            _solutionEvents.ProjectRemoved += OnProjectRemoved;
            _solutionEvents.BeforeClosing += OnBeforeClosing;
        }

        public IPackageRepository SolutionRepository {
            get {
                return LocalRepository;
            }
        }

        public IPackageRepository ExternalRepository {
            get {
                return SourceRepository;
            }
        }

        private IEnumerable<ProjectManager> ProjectManagers {
            get {
                EnsureProjectManagers();
                return _projectManagers.Values;
            }
        }

        // Need object overloads so that the powershell script can call into it
        public ProjectManager GetProjectManager(object project) {
            return GetProjectManager((Project)project);
        }

        public ProjectManager GetProjectManager(Project project) {
            EnsureProjectManagers();
            ProjectManager projectManager;
            _projectManagers.TryGetValue(project, out projectManager);
            return projectManager;
        }

        private void EnsureProjectManagers() {
            // Cache the list of projects
            if (_projectManagers == null) {
                _projectManagers = new Dictionary<Project, ProjectManager>();

                foreach (Project project in GetAllProjects()) {
                    // Create a project manager for each of the projects
                    var projectManager = CreateProjectManager(project);

                    _projectManagers.Add(project, projectManager);
                }
            }
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

        public override void UninstallPackage(Package package, bool force = false, bool removeDependencies = false) {
            // Remove reference from projects that reference this package
            var projectManagers = GetProjectsWithPackage(package.Id, package.Version);
            if (projectManagers.Any()) {
                // We don't need to actually call uninstall since uninstalling it from all the projects
                // already has a side effect of removing it from the package manager
                foreach (ProjectManager projectManager in projectManagers) {
                    projectManager.RemovePackageReference(package.Id, force, removeDependencies);
                }
            }
            else {
                base.UninstallPackage(package, force, removeDependencies);
            }
        }

        internal void OnPackageReferenceRemoved(Package removedPackage, bool force = false, bool removeDependencies = false) {
            if (!IsPackageReferenced(removedPackage)) {
                // There are no packages that depend on this one so just uninstall it
                base.UninstallPackage(removedPackage.Id, removedPackage.Version, force, removeDependencies);
            }
        }

        private bool IsPackageReferenced(Package package) {
            return GetProjectsWithPackage(package.Id, package.Version).Any();
        }

        // Need object overloads so that the powershell script can call into it
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dte", Justification = "dte is the vs automation object")]
        public static VSPackageManager GetPackageManager(string source, object dte) {
            return GetPackageManager(source, (DTE)dte);
        }

        private static VSPackageManager GetPackageManager(string source, DTE dte) {
            // We're caching a package manager per source since we can't change the source after the fact (right now at least)
            VSPackageManager packageManager;
            if (!_packageManagerCache.TryGetValue(source, out packageManager)) {
                // Create a repository for this source
                IPackageRepository repository = PackageRepositoryFactory.CreateRepository(source);

                // Get the file system for the solution folder
                var solutionFileSystem = new SolutionFolderProjectSystem(dte.Solution, "packages");

                // Create a new vs package manager
                packageManager = new VSPackageManager(dte, repository, solutionFileSystem);

                // Add it to the cache
                _packageManagerCache.TryAdd(source, packageManager);
            }
            return packageManager;
        }

        private ProjectManager CreateProjectManager(Project project) {
            var assemblyPathResolver = new DefaultPackageAssemblyPathResolver(FileSystem, ReferencesDirectory);
            var projectManager = new VSProjectManager(this, assemblyPathResolver, project);
            projectManager.Listener = Listener;
            return projectManager;
        }

        private IEnumerable<ProjectManager> GetProjectsWithPackage(string packageId, Version version) {
            return from projectManager in ProjectManagers
                   let package = projectManager.GetPackageReference(packageId)
                   where package != null && (version == null || (version != null && package.Version.Equals(version)))
                   select projectManager;
        }

        private void OnBeforeClosing() {
            // Invalidate our cache on closing
            _projectManagers = null;
        }

        private void OnProjectRemoved(Project project) {
            if (_projectManagers != null) {
                _projectManagers.Remove(project);
            }
        }

        private void OnProjectAdded(Project project) {
            // Only add supported projects
            if (IsSupportedProject(project)) {
                EnsureProjectManagers();
                // If _projectManagers was null then EnsureProjectManagers would have populated 
                // the cache with this project already.
                if (!_projectManagers.ContainsKey(project)) {
                    _projectManagers.Add(project, CreateProjectManager(project));
                }
            }
        }

        // We build a cache of all projects recrusively. We need to do this since solution folders
        // are seen by the dte object model as projects and the actual projects are normally within them.
        private IEnumerable<Project> GetAllProjects() {
            var projects = new Stack<Project>();
            foreach (Project project in _dte.Solution.Projects) {
                projects.Push(project);
            }

            while (projects.Any()) {
                Project project = projects.Pop();

                if (IsSupportedProject(project)) {
                    yield return project;
                }

                foreach (ProjectItem projectItem in project.ProjectItems) {
                    if (projectItem.SubProject != null) {
                        projects.Push(projectItem.SubProject);
                    }
                }
            }
        }

        private static bool IsSupportedProject(Project project) {
            return project.Kind != null &&
                   _supportedProjectTypes.Contains(project.Kind, StringComparer.OrdinalIgnoreCase);
        }
    }
}
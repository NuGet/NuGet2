using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ISolutionManager))]
    public class SolutionManager : ISolutionManager {
        private readonly DTE _dte;
        private readonly SolutionEvents _solutionEvents;

        private ProjectCache _projectCache;

        public SolutionManager()
            : this(ServiceLocator.GetInstance<DTE>()) {
        }

        private SolutionManager(DTE dte) {
            if (dte == null) {
                throw new ArgumentNullException("dte");
            }

            _dte = dte;

            // Keep a reference to SolutionEvents so that it doesn't get GC'ed. Otherwise, we won't receive events.
            _solutionEvents = _dte.Events.SolutionEvents;
            _solutionEvents.Opened += OnSolutionOpened;
            _solutionEvents.BeforeClosing += OnBeforeClosing;
            _solutionEvents.ProjectAdded += OnProjectAdded;
            _solutionEvents.ProjectRemoved += OnProjectRemoved;
            _solutionEvents.ProjectRenamed += OnProjectRenamed;

            if (_dte.Solution.IsOpen) {
                OnSolutionOpened();
            }
        }

        public string DefaultProjectName {
            get;
            set;
        }

        public Project DefaultProject {
            get {
                if (String.IsNullOrEmpty(DefaultProjectName)) {
                    return null;
                }
                Project project = GetProject(DefaultProjectName);
                Debug.Assert(project != null, "Invalid default project");
                return project;
            }
        }

        public event EventHandler SolutionOpened;

        public event EventHandler SolutionClosing;

        /// <summary>
        /// Gets a value indicating whether there is a solution open in the IDE.
        /// </summary>
        public bool IsSolutionOpen {
            get {
                return _dte != null && _dte.Solution != null && _dte.Solution.IsOpen;
            }
        }

        public string SolutionDirectory {
            get {
                if (!IsSolutionOpen) {
                    return null;
                }
                // Use .Properties.Item("Path") instead of .FullName because .FullName might not be
                // available if the solution is just being created
                Property property = _dte.Solution.Properties.Item("Path");
                if (property == null) {
                    return null;
                }
                string solutionFilePath = property.Value;
                if (String.IsNullOrEmpty(solutionFilePath)) {
                    return null;
                }
                return Path.GetDirectoryName(solutionFilePath);
            }
        }

        /// <summary>
        /// Gets a list of supported projects currently loaded in the solution
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This method is potentially expensive if the cache is not constructed yet.")]
        public IEnumerable<Project> GetProjects() {
            if (IsSolutionOpen) {
                EnsureProjectCache();
                return _projectCache.GetProjects();
            }
            else {
                return Enumerable.Empty<Project>();
            }
        }

        public string GetProjectSafeName(Project project) {
            if (project == null) {
                throw new ArgumentNullException("project");
            }

            // try searching for simple names first
            string name = project.Name;
            if (GetProject(name) == project) {
                return name;
            }

            return project.GetCustomUniqueName();
        }

        public Project GetProject(string projectSafeName) {
            if (IsSolutionOpen) {
                EnsureProjectCache();

                Project project;
                if (_projectCache.TryGetProject(projectSafeName, out project)) {
                    return project;
                }
            }

            return null;
        }

        private void OnBeforeClosing() {
            DefaultProjectName = null;
            _projectCache = null;
            if (SolutionClosing != null) {
                SolutionClosing(this, EventArgs.Empty);
            }
        }

        private void OnProjectRenamed(Project project, string oldName) {
            if (!String.IsNullOrEmpty(oldName)) {
                if (project.IsSupported()) {
                    EnsureProjectCache();

                    RemoveProjectFromCache(oldName);
                    AddProjectToCache(project);
                }
            }
        }

        private void OnProjectRemoved(Project project) {
            RemoveProjectFromCache(project.UniqueName);
        }

        private void OnProjectAdded(Project project) {
            if (project.IsSupported()) {
                EnsureProjectCache();
                AddProjectToCache(project);
            }
        }

        private void OnSolutionOpened() {
            EnsureProjectCache();
            SetDefaultProject();
            if (SolutionOpened != null) {
                SolutionOpened(this, EventArgs.Empty);
            }
        }

        private void EnsureProjectCache() {
            if (IsSolutionOpen && _projectCache == null) {
                _projectCache = new ProjectCache();

                var allProjects = _dte.Solution.GetAllProjects();
                foreach (Project project in allProjects) {
                    AddProjectToCache(project);
                }
            }
        }

        private void AddProjectToCache(Project project) {
            ProjectName collidingProjectName;
            ProjectName projectName;

            _projectCache.TryAddProject(project, out collidingProjectName, out projectName);

            // Set the DefaultProjectName if it's not set or 
            // If the default project is the colliding one then make sure it's using the unique name
            if (projectName.SimpleName.Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase) ||
                String.IsNullOrEmpty(DefaultProjectName)) {
                DefaultProjectName = collidingProjectName != null ?
                                     collidingProjectName.CustomUniqueName :
                                     projectName.SimpleName;
            }
        }

        private void RemoveProjectFromCache(string name) {
            // Remove the entry from both dictionaries
            _projectCache.RemoveProject(name);

            if (!_projectCache.Contains(DefaultProjectName)) {
                DefaultProjectName = String.Empty;
            }
        }

        private void SetDefaultProject() {
            // when a new solution opens, we set its startup project as the default project in NuGet Console
            var solutionBuild = (SolutionBuild2)_dte.Solution.SolutionBuild;
            if (solutionBuild.StartupProjects != null) {
                IEnumerable<object> startupProjects = solutionBuild.StartupProjects;
                string startupProjectName = startupProjects.Cast<string>().FirstOrDefault();
                if (!String.IsNullOrEmpty(startupProjectName)) {
                    // startupProjectName matches the UniqueName property of Project class. 
                    // We want to extract the Name property of the startup Project instead.
                    DefaultProjectName = GetProjectName(startupProjectName);
                }
            }
        }

        /// <summary>
        /// Search through the solution to look for a matching project with startupProject
        /// </summary>
        private string GetProjectName(string startupProjectName) {
            Project project = GetProject(startupProjectName);

            if (project != null) {
                return GetProjectSafeName(project);
            }

            return null;
        }

        // REVIEW: This might be inefficient, see what we can do with caching projects until references change
        public IEnumerable<Project> GetDependentProjects(Project project) {
            if (project == null) {
                throw new ArgumentNullException("project");
            }

            var dependentProjects = new Dictionary<string, List<Project>>();

            // Get all of the projects in the solution and build the reverse graph. i.e.
            // if A has a project reference to B (A -> B) the this will return B -> A
            // We need to run this on the ui thread so that it doesn't freeze for websites. Since there might be a 
            // large number of references.           
            ThreadHelper.Generic.Invoke(() => {
                foreach (var proj in GetProjects()) {
                    foreach (var referencedProject in proj.GetReferencedProjects()) {
                        AddDependentProject(dependentProjects, referencedProject, proj);
                    }
                }
            });

            List<Project> dependents;
            if (dependentProjects.TryGetValue(project.UniqueName, out dependents)) {
                return dependents;
            }

            return Enumerable.Empty<Project>();
        }

        private static void AddDependentProject(IDictionary<string, List<Project>> dependentProjects,
                                         Project project,
                                         Project dependent) {
            List<Project> dependents;
            if (!dependentProjects.TryGetValue(project.UniqueName, out dependents)) {
                dependents = new List<Project>();
                dependentProjects[project.UniqueName] = dependents;
            }
            dependents.Add(dependent);
        }

        /// <summary>
        /// Cache that stores project based on multiple names. i.e. Project can be retrieved by name (if non conflicting), unique name or
        /// full path.
        /// </summary>
        private class ProjectCache {
            // Mapping from project name structure to project instance
            private Dictionary<ProjectName, Project> _projectCache = new Dictionary<ProjectName, Project>();

            // Mapping from all names to a project name structure
            private Dictionary<string, ProjectName> _projectNamesCache = new Dictionary<string, ProjectName>(StringComparer.OrdinalIgnoreCase);

            public bool TryGetProject(string name, out Project project) {
                ProjectName projectName;
                if (_projectNamesCache.TryGetValue(name, out projectName)) {
                    if (_projectCache.TryGetValue(projectName, out project)) {
                        return true;
                    }
                }
                project = null;
                return false;
            }

            public void RemoveProject(string name) {
                ProjectName projectName;
                if (_projectNamesCache.TryGetValue(name, out projectName)) {
                    RemoveEntry(projectName);
                }
            }

            private void RemoveEntry(ProjectName projectName) {
                _projectNamesCache.Remove(projectName.CustomUniqueName);
                _projectNamesCache.Remove(projectName.FullName);

                // Only remove the simple name if the custom unique name matches. This is so we avoid
                // the scenario where 2 projects have the same simple name and one of them is removed.
                // We still need to be able to refer to it by simple name
                if (projectName.CustomUniqueName == projectName.SimpleName) {
                    _projectNamesCache.Remove(projectName.SimpleName);
                }

                _projectNamesCache.Remove(projectName.UniqueName);
                _projectCache.Remove(projectName);
            }

            public bool Contains(string name) {
                return _projectNamesCache.ContainsKey(name);
            }

            public IEnumerable<Project> GetProjects() {
                return _projectCache.Values;
            }

            // Try to add a project to the cache returning any collisions in simple names
            public bool TryAddProject(Project project,
                                      out ProjectName collidingProjectName,
                                      out ProjectName projectName) {
                collidingProjectName = null;

                // First create a project name from the project
                projectName = new ProjectName(project);

                if (_projectCache.ContainsKey(projectName)) {
                    return false;
                }

                string name = project.Name;
                string uniqueName = project.GetCustomUniqueName();

                // If there is a name collision then remove the name from the dictionary
                if (_projectNamesCache.TryGetValue(name, out collidingProjectName)) {
                    if (collidingProjectName.CustomUniqueName != collidingProjectName.SimpleName) {
                        _projectNamesCache.Remove(name);
                    }
                }
                else {
                    _projectNamesCache[name] = projectName;
                }

                _projectNamesCache[uniqueName] = projectName;
                _projectNamesCache[project.UniqueName] = projectName;
                _projectNamesCache[project.FullName] = projectName;

                // Add the entry mapping project name to the actual project
                _projectCache[projectName] = project;

                return true;
            }
        }

        private class ProjectName : IEquatable<ProjectName> {
            public ProjectName(Project project) {
                FullName = project.FullName;
                UniqueName = project.UniqueName;
                SimpleName = project.Name;
                CustomUniqueName = project.GetCustomUniqueName();
            }

            public string FullName { get; set; }
            public string UniqueName { get; set; }
            public string SimpleName { get; set; }
            public string CustomUniqueName { get; set; }

            public bool Equals(ProjectName other) {
                return other.UniqueName.Equals(other.UniqueName, StringComparison.OrdinalIgnoreCase);
            }

            public override int GetHashCode() {
                return UniqueName.GetHashCode();
            }

            public override string ToString() {
                return UniqueName;
            }
        }
    }
}
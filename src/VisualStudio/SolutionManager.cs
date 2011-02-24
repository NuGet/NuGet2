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

        // this dictionary stores projects by simple name
        private Dictionary<string, HashSet<Project>> _projectCacheByName;
        // this dictionary stores projects by unique name
        private Dictionary<string, Project> _projectCacheByUniqueName;

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
                if (!IsSolutionOpen ) {
                    return null;
                }
                // Use .Properties.Item("Path") instead of .FullName because .FullName might not be
                // available if the solution is just being created
                Property property = _dte.Solution.Properties.Item("Path");
                if(property == null) {
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
                return _projectCacheByUniqueName.Values;
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

                Project project = null;
                if (_projectCacheByUniqueName.TryGetValue(projectSafeName, out project)) {
                    return project;
                }

                HashSet<Project> allProjects;
                if (_projectCacheByName.TryGetValue(projectSafeName, out allProjects)) {
                    if (allProjects.Count == 1) {
                        return allProjects.Single();
                    }
                }
            }
            return null;
        }

        private void OnBeforeClosing() {
            DefaultProjectName = null;
            _projectCacheByName = null;
            _projectCacheByUniqueName = null;
            if (SolutionClosing != null) {
                SolutionClosing(this, EventArgs.Empty);
            }
        }

        private void OnProjectRenamed(Project project, string oldName) {
            if (!String.IsNullOrEmpty(oldName)) {
                
                if (project.IsSupported()) {
                    EnsureProjectCache();

                    // oldName is the full path to the project file. Need to convert it to simple project name. 
                    // No need to worry about Website project here, because there is no option to rename Website project.
                    string simpleOldName = Path.GetFileNameWithoutExtension(oldName);

                    RemoveProjectByKey(project, simpleOldName);
                    AddProjectToCaches(project);
                }
            }
        }

        private void OnProjectRemoved(Project project) {
            RemoveProjectFromCaches(project);
        }

        private void OnProjectAdded(Project project) {
            if (project.IsSupported()) {
                EnsureProjectCache();
                AddProjectToCaches(project);
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
            if (IsSolutionOpen && _projectCacheByName == null) {
                _projectCacheByUniqueName = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);
                _projectCacheByName = new Dictionary<string, HashSet<Project>>(StringComparer.OrdinalIgnoreCase);

                var allProjects = _dte.Solution.GetAllProjects();
                foreach (Project project in allProjects) {
                    AddProjectToCaches(project);
                }
            }
        }

        private void AddProjectToCaches(Project project) {
            // add to the unique name dictionary
            string uniqueName = project.GetCustomUniqueName();
            _projectCacheByUniqueName[uniqueName] = project;

            // add to simple name dictionary
            string name = project.Name;
            HashSet<Project> allProjects;
            if (!_projectCacheByName.TryGetValue(name, out allProjects)) {
                allProjects = new HashSet<Project>();
                _projectCacheByName.Add(name, allProjects);
            }

            // If there is currently only one project with this simple name, and it's the 
            // default project, we have to set default project name to the unique name because
            // we are having a name collision here.
            if (allProjects.Count == 1 && 
                name.Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase)) {
                Project defaultProject = allProjects.Single();
                DefaultProjectName = defaultProject.GetCustomUniqueName();
            }

            // now add our project to the simple dictionary
            allProjects.Add(project);

            // Set the DefaultProjectName if it's not set.
            // Try to use simple name if there is no conflict (count = 1), otherwise use uniquename
            if (String.IsNullOrEmpty(DefaultProjectName)) {
                DefaultProjectName = allProjects.Count == 1 ? name : uniqueName;
            }
        }

        private void RemoveProjectByKey(Project project, string oldName) {
            // Get the old unique name of the project.
            // Calling GetCustomUniqueName() here is wrong, because the project has been renamed, 
            // and hence it has the new unique name now. We want to get the old unique name that
            // is stored in the dictionary.
            string uniqueName = _projectCacheByUniqueName.
                Where(pair => pair.Value == project).
                Select(pair => pair.Key).
                FirstOrDefault();

            RemoveProjectFromCaches(uniqueName ?? String.Empty, oldName, project);
        }

        private void RemoveProjectFromCaches(Project project) {
            RemoveProjectFromCaches(project.GetCustomUniqueName(), project.Name, project);
        }
        
        private void RemoveProjectFromCaches(string uniqueName, string name, Project project) {
            // deal with unique name
            _projectCacheByUniqueName.Remove(uniqueName);

            // now with the simple name
            HashSet<Project> allProjects;
            if (_projectCacheByName.TryGetValue(name, out allProjects)) {
                allProjects.Remove(project);

                if (allProjects.Count == 0) {
                    _projectCacheByName.Remove(name);
                }
                else if (allProjects.Count == 1) {
                    // the DefaultProjectName is currently set to this project's unique name, change it to simple name
                    // because there is no more collision after the removal.
                    if (allProjects.Single().GetCustomUniqueName().Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase)) {
                        DefaultProjectName = name;
                    }
                }
            }

            if (!_projectCacheByName.ContainsKey(DefaultProjectName) &&
                !_projectCacheByUniqueName.ContainsKey(DefaultProjectName)) {
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
            return (from project in _projectCacheByUniqueName.Values
                    where project.UniqueName.Equals(startupProjectName, StringComparison.OrdinalIgnoreCase)
                    select GetProjectSafeName(project)).FirstOrDefault();
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
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ISolutionManager))]
    public class SolutionManager : ISolutionManager {
        private readonly DTE _dte;
        private readonly SolutionEvents _solutionEvents;

        // this dictionary stores projects by simple name
        private Dictionary<string, Project> _projectCacheByName;
        // this dictionary stores projects by unique name
        private Dictionary<string, Project> _projectCacheByUniqueName;
        private EventHandler _solutionOpened;
        private EventHandler _solutionClosing;

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

        public event EventHandler SolutionOpened {
            add {
                _solutionOpened += value;
            }
            remove {
                _solutionOpened -= value;
            }
        }

        public event EventHandler SolutionClosing {
            add {
                _solutionClosing += value;
            }
            remove {
                _solutionClosing -= value;
            }
        }

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

        public IEnumerable<string> GetProjectSafeNames() {
            foreach (var pair in _projectCacheByUniqueName) {
                string name = pair.Value.Name;
                if (_projectCacheByName.ContainsKey(name)) {
                    // if possible, return simple name
                    yield return name;
                }
                else {
                    // otherwise, return unique name
                    yield return pair.Key;
                }
            }
        }

        public string GetSafeName(Project project) {
            // try searching for simple names first
            var name = _projectCacheByName.Where(p => p.Value == project).Select(p => p.Key).FirstOrDefault();
            if (name != null) {
                return name;
            }

            // now search for for unique names
            return _projectCacheByUniqueName.Where(p => p.Value == project).Select(p => p.Key).FirstOrDefault();
        }

        public Project GetProject(string projectName) {
            if (IsSolutionOpen) {
                EnsureProjectCache();

                Project project = null;
                if (_projectCacheByUniqueName.TryGetValue(projectName, out project)) {
                    return project;
                }

                _projectCacheByName.TryGetValue(projectName, out project);
                return project;
            }
            else {
                return null;
            }
        }

        private void OnBeforeClosing() {
            DefaultProjectName = null;
            _projectCacheByName = null;
            _projectCacheByUniqueName = null;
            if (_solutionClosing != null) {
                _solutionClosing(this, EventArgs.Empty);
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
            if (_solutionOpened != null) {
                _solutionOpened(this, EventArgs.Empty);
            }
        }

        private void EnsureProjectCache() {
            if (IsSolutionOpen && _projectCacheByName == null) {
                _projectCacheByUniqueName = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);
                _projectCacheByName = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);

                var allProjects = _dte.Solution.GetAllProjects();
                foreach (Project project in allProjects) {
                    AddProjectToCaches(project);
                }
            }
        }

        private void AddProjectToCaches(Project project) {
            // always add it to the unique name dictionary
            string uniqueName = project.GetCustomUniqueName();
            _projectCacheByUniqueName[uniqueName] = project;

            string name = project.Name;
            if (_projectCacheByName.ContainsKey(name)) {
                // There is a name collision here. If so, remove the previous Project from the simple name dictionary.
                _projectCacheByName.Remove(name);
            }
            else {
                // Otherwise, add it to the simple name dictionary
                _projectCacheByName.Add(name, project);
            }

            // set the DefaultProjectName if it's not set
            if (String.IsNullOrEmpty(DefaultProjectName)) {
                DefaultProjectName = _projectCacheByName.ContainsKey(name) ? name : uniqueName;
            }
        }

        private void RemoveProjectByKey(Project project, string oldName) {
            // get the unique name of the project
            string uniqueName = _projectCacheByUniqueName.
                Where(pair => pair.Value == project).
                Select(pair => pair.Key).
                FirstOrDefault();

            RemoveProjectFromCaches(uniqueName ?? String.Empty, oldName);
        }

        private void RemoveProjectFromCaches(Project project) {
            RemoveProjectFromCaches(project.GetCustomUniqueName(), project.Name);
        }
        
        private void RemoveProjectFromCaches(string uniqueName, string name) {
            _projectCacheByUniqueName.Remove(uniqueName);

            if (_projectCacheByName.ContainsKey(name)) {
                _projectCacheByName.Remove(name);
            }
            else {
                var candidates =
                    _projectCacheByUniqueName.Values.Where(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();

                // if after removal, there is exactly one Projet left with the that simple name, 
                // add it to the simple name dictionary
                if (candidates.Count == 1) {
                    _projectCacheByName.Add(name, candidates[0]);

                    // the DefaultProjectName is currently set to this project's unique name, change it to simple name
                    if (candidates[0].GetCustomUniqueName().Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase)) {
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
                    select project.Name).FirstOrDefault();
        }
    }
}
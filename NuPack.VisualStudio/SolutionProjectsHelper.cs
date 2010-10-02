using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace NuPack.VisualStudio {

    public class SolutionProjectsHelper : INotifyPropertyChanged {
        private static SolutionProjectsHelper _instance;
        private static readonly object _lock = new object();

        private Dictionary<string, Project> _projectCache = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);
        private DTE2 _dte;
        private SolutionEvents _solutionEvents;
        private string _defaultProjectName;

        public static SolutionProjectsHelper Instance {
            get {
                if (_instance == null) {
                    lock (_lock) {
                        if (_instance == null) {
                            _instance = new SolutionProjectsHelper((DTE2)DTEExtensions.DTE);
                        }
                    }
                }

                return _instance;
            }
        }

        private SolutionProjectsHelper(DTE2 dte) {
            if (dte == null) {
                throw new ArgumentNullException("dte");
            }

            _dte = dte;
            RegisterSolutionEvents();
        }

        public string DefaultProjectName {
            get {
                return _defaultProjectName;
            }
            set {
                if (_defaultProjectName != value) {
                    _defaultProjectName = value;
                    NotifyPropertyChange("DefaultProjectName");
                }
            }
        }

        private void RegisterSolutionEvents() {
            var events = _dte.Events.SolutionEvents;
            events.Opened += new _dispSolutionEvents_OpenedEventHandler(OnNewSolutionOpened);
            events.BeforeClosing += new _dispSolutionEvents_BeforeClosingEventHandler(OnBeforeClosing);
            events.ProjectAdded += new _dispSolutionEvents_ProjectAddedEventHandler(OnProjectAdded);
            events.ProjectRemoved += new _dispSolutionEvents_ProjectRemovedEventHandler(OnProjectRemoved);
            events.ProjectRenamed += new _dispSolutionEvents_ProjectRenamedEventHandler(OnProjectRenamed);

            if (_dte.Solution.IsOpen) {
                OnNewSolutionOpened();
            }

            // keep a reference to SolutionEvents so that it doesn't get GC'ed. Otherwise, we won't receive events.
            _solutionEvents = events;
        }

        private void OnBeforeClosing() {
            DefaultProjectName = null;
            _projectCache.Clear();
        }

        private void OnProjectRenamed(Project project, string oldName) {
            if (IsProjectSupported(project)) {
                _projectCache[project.Name] = project;
                _projectCache.Remove(oldName);
            }

            if (oldName != null) {
                // oldName is the full path to the project file. Need to convert it to simple project name. 
                // No need to worry about Website project here, because there is no option to rename Website project.
                oldName = System.IO.Path.GetFileNameWithoutExtension(oldName);
                if (oldName.Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase)) {
                    DefaultProjectName = project.Name;
                }
            }
        }

        private void OnProjectRemoved(Project project) {
            _projectCache.Remove(project.Name);

            if (project.Name.Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase)) {
                DefaultProjectName = String.Empty;
            }
        }

        private void OnProjectAdded(Project project) {
            if (IsProjectSupported(project)) {
                _projectCache[project.Name] = project;
            }

            if (String.IsNullOrEmpty(DefaultProjectName)) {
                DefaultProjectName = project.Name;
            }
        }

        private void OnNewSolutionOpened() {
            SetupCacheForNewSolution();
            SetDefaultProject();
        }

        private void SetupCacheForNewSolution() {
            foreach (var project in GetAllSupportedProjects()) {
                _projectCache[project.Name] = project;
            }
        }

        private void SetDefaultProject() {
            // when a new solution opens, we set its startup project as the default project in NuPack Console
            SolutionBuild2 sb = (SolutionBuild2)_dte.Solution.SolutionBuild;
            Array projects = (Array)sb.StartupProjects;
            if (projects != null && projects.Length > 0) {
                string startupProject = null;
                foreach (string item in projects) {
                    startupProject = item;
                    break;
                }

                Debug.Assert(startupProject != null);

                // startupProject matches the UniqueName property of Project class. 
                // We want to extract the Name property of the startup Project instead.
                DefaultProjectName = SearchForProjectName(startupProject);
            }
        }

        /// <summary>
        /// Recursively search through the solution to look for a matching project with startupProject
        /// </summary>
        private string SearchForProjectName(string startupProject)
        {
            var p = (from Project project in _projectCache.Values
                    where project.UniqueName.Equals(startupProject, StringComparison.OrdinalIgnoreCase)
                    select project).FirstOrDefault();

            return (p == null) ? null : p.Name;
        }

        /// <summary>
        /// Gets the list of names of all supported projects currently loaded in the solution
        /// </summary>
        /// <returns></returns>
        public string[] GetCurrentProjectNames() {
            if (_dte.Solution.IsOpen) {
                return (from p in _projectCache.Values select p.Name).ToArray();
            }
            else {
                return new string[0];
            }
        }

        public Project GetProjectFromName(string projectName) {
            Project project;
            _projectCache.TryGetValue(projectName, out project);
            return project;
        }

        /// <summary>
        /// Return the list of all supported projects in the current solution. This method
        /// recursively iterates through all projects.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Project> GetAllSupportedProjects() {
            if (_dte.Solution == null || !_dte.Solution.IsOpen) {
                yield break;
            }

            Queue<Project> ps = new Queue<Project>();
            foreach (Project project in _dte.Solution.Projects) {
                ps.Enqueue(project);
            }

            while (ps.Count > 0) {
                Project project = ps.Dequeue();
                if (IsProjectSupported(project)) {
                    yield return project;
                }

                foreach (ProjectItem pi in project.ProjectItems) {
                    if (pi.SubProject != null) {
                        ps.Enqueue(pi.SubProject);
                    }
                }
            }
        }

        private static readonly string[] SupportedProjectTypes = new string[] {
            VSConstants.CsharpProjectKind,
            VSConstants.VbProjectKind,
            VSConstants.WebSiteProjectKind
        };

        private static bool IsProjectSupported(Project project) {
            return SupportedProjectTypes.Contains(project.Kind, StringComparer.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChange(string propertyName) {
            var handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
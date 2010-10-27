namespace NuGet.VisualStudio {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using EnvDTE;
    using EnvDTE80;

    public class SolutionManager : ISolutionManager {
        private static readonly Lazy<SolutionManager> _instance = new Lazy<SolutionManager>(() => new SolutionManager(DTEExtensions.DTE));
        private readonly DTE _dte;
        private readonly SolutionEvents _solutionEvents;

        private Dictionary<string, Project> _projectCache;

        // REVIEW: Instance would be more appropriate than Current
        public static SolutionManager Current {
            get {
                return _instance.Value;
            }
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

        /// <summary>
        /// Gets a value indicating whether there is a solution open in the IDE.
        /// </summary>
        public bool IsSolutionOpen {
            get {
                return _dte != null && _dte.Solution != null && _dte.Solution.IsOpen;
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
                return _projectCache.Values;
            }
            else {
                return Enumerable.Empty<Project>();
            }
        }

        public Project GetProject(string projectName) {
            if (IsSolutionOpen) {
                EnsureProjectCache();
                Project project;
                _projectCache.TryGetValue(projectName, out project);
                return project;
            }
            else {
                return null;
            }
        }


        private void OnBeforeClosing() {
            DefaultProjectName = null;
            _projectCache = null;
        }

        private void OnProjectRenamed(Project project, string oldName) {
            if (!String.IsNullOrEmpty(oldName)) {
                // oldName is the full path to the project file. Need to convert it to simple project name. 
                // No need to worry about Website project here, because there is no option to rename Website project.
                oldName = Path.GetFileNameWithoutExtension(oldName);

                if (project.IsSupported()) {
                    EnsureProjectCache();

                    _projectCache[project.Name] = project;
                    _projectCache.Remove(oldName);
                }

                if (oldName.Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase)) {
                    DefaultProjectName = project.Name;
                }
            }
        }

        private void OnProjectRemoved(Project project) {
            if (_projectCache != null) {
                _projectCache.Remove(project.Name);
            }

            if (project.Name.Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase)) {
                DefaultProjectName = String.Empty;
            }
        }

        private void OnProjectAdded(Project project) {
            if (project.IsSupported()) {
                EnsureProjectCache();
                _projectCache[project.Name] = project;

                if (String.IsNullOrEmpty(DefaultProjectName)) {
                    DefaultProjectName = project.Name;
                }
            }
        }

        private void OnSolutionOpened() {
            EnsureProjectCache();
            SetDefaultProject();
        }

        private void EnsureProjectCache() {
            if (IsSolutionOpen && _projectCache == null) {
                // Initialize the cache
                var allProjects = _dte.Solution.GetAllProjects();
                _projectCache = allProjects.ToDictionary(project => project.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        private void SetDefaultProject() {
            // when a new solution opens, we set its startup project as the default project in NuGet Console
            var solutionBuild = (SolutionBuild2)_dte.Solution.SolutionBuild;
            if (solutionBuild.StartupProjects != null) {
                IEnumerable<object> startupProjects = solutionBuild.StartupProjects;
                string startupProjectName = startupProjects.Cast<string>()
                                                           .FirstOrDefault();

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
            return (from project in _projectCache.Values
                    where project.UniqueName.Equals(startupProjectName, StringComparison.OrdinalIgnoreCase)
                    select project.Name).FirstOrDefault();
        }
    }
}

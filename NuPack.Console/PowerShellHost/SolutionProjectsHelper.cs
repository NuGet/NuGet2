using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace NuPackConsole.Host.PowerShell.Implementation {
    internal class SolutionProjectsHelper {
        private DTE2 _dte;
        private IHost _host;
        private SolutionEvents _solutionEvents;
 
        private static readonly string[] SupportedProjectTypes = new string[] {
            VSConstants.CsharpProjectKind,
            VSConstants.VbProjectKind,
            VSConstants.WebSiteProjectKind
        };

        public SolutionProjectsHelper(DTE2 dte, IHost host) {
            UtilityMethods.ThrowIfArgumentNull(dte);
            UtilityMethods.ThrowIfArgumentNull(host);

            _dte = dte;
            _host = host;
        }

        public void RegisterSolutionEvents() {
            var events = _dte.Events.SolutionEvents;
            events.Opened += new _dispSolutionEvents_OpenedEventHandler(OnNewSolutionOpened);
            events.ProjectAdded += new _dispSolutionEvents_ProjectAddedEventHandler(OnProjectAdded);
            events.ProjectRemoved += new _dispSolutionEvents_ProjectRemovedEventHandler(OnProjectRemoved);
            events.ProjectRenamed += new _dispSolutionEvents_ProjectRenamedEventHandler(OnProjectRenamed);

            if (_dte.Solution.IsOpen) {
                OnNewSolutionOpened();
            }

            // keep a reference to SolutionEvents so that it doesn't get GC'ed. Otherwise, we won't receive events
            _solutionEvents = events;
        }

        private void OnProjectRenamed(Project project, string oldName) {
            if (oldName != null) {
                // oldName is the full path to the project file. Need to convert it to simple project name. 
                // No need to worry about Website project here, because there is no option to rename Website project.
                oldName = System.IO.Path.GetFileNameWithoutExtension(oldName);
                if (oldName.Equals(_host.DefaultProject, StringComparison.OrdinalIgnoreCase)) {
                    _host.DefaultProject = project.Name;
                }
            }
        }

        private void OnProjectRemoved(Project project) {
            if (project.Name.Equals(_host.DefaultProject, StringComparison.OrdinalIgnoreCase)) {
                _host.DefaultProject = String.Empty;
            }
        }

        private void OnProjectAdded(Project project) {
            if (String.IsNullOrEmpty(_host.DefaultProject)) {
                _host.DefaultProject = project.Name;
            }
        }

        private void OnNewSolutionOpened() {
            // when a new solution opens, we set its startup project as the default project in NuPack Console
            SolutionBuild2 sb = (SolutionBuild2)_dte.Solution.SolutionBuild;
            Array projects = (Array)sb.StartupProjects;
            if (projects != null && projects.Length > 0) {
                string startupProject = null;
                foreach (string item in projects)
                {
                    startupProject = item;
                    break;
                }

                Debug.Assert(startupProject != null);

                // startupProject matches the UniqueName property of Project class. 
                // We want to extract the Name property of the startup Project instead.
                _host.DefaultProject = SearchForProjectName(startupProject);
            }
        }

        /// <summary>
        /// Recursively search through the solution to look for a matching project with startupProject
        /// </summary>
        private string SearchForProjectName(string startupProject)
        {
            var p = (from Project project in GetAllSupportedProjects()
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
                return (from p in GetAllSupportedProjects() select p.Name).ToArray();
            }
            else {
                return new string[0];
            }
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

        private static bool IsProjectSupported(Project project) {
            return SupportedProjectTypes.Contains(project.Kind, StringComparer.OrdinalIgnoreCase);
        }
    }
}
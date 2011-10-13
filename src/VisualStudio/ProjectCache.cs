using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// Cache that stores project based on multiple names. i.e. Project can be retrieved by name (if non conflicting), unique name and custom unique name.
    /// </summary>
    internal class ProjectCache
    {
        // Mapping from project name structure to project instance
        private readonly Dictionary<ProjectName, Project> _projectCache = new Dictionary<ProjectName, Project>();

        // Mapping from all names to a project name structure
        private readonly Dictionary<string, ProjectName> _projectNamesCache = new Dictionary<string, ProjectName>(StringComparer.OrdinalIgnoreCase);

        // We need another dictionary for short names since there may be more than project name per short name
        private readonly Dictionary<string, HashSet<ProjectName>> _shortNameCache = new Dictionary<string, HashSet<ProjectName>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Finds a project by short name, unique name or custom unique name.
        /// </summary>
        /// <param name="name">name of the project to retrieve.</param>
        /// <param name="project">project instance</param>
        /// <returns>true if the project with the specified name is cached.</returns>
        public bool TryGetProject(string name, out Project project)
        {
            project = null;
            // First try to find the project name in one of the dictionaries. Then locate the project for that name.
            ProjectName projectName;
            return TryGetProjectName(name, out projectName) &&
                   _projectCache.TryGetValue(projectName, out project);
        }

        /// <summary>
        /// Finds a project name by short name, unique name or custom unique name.
        /// </summary>
        /// <param name="name">name of the project</param>
        /// <param name="projectName">project name instance</param>
        /// <returns>true if the project name with the specified name is found.</returns>
        public bool TryGetProjectName(string name, out ProjectName projectName)
        {
            return _projectNamesCache.TryGetValue(name, out projectName) ||
                   TryGetProjectNameByShortName(name, out projectName);
        }

        /// <summary>
        /// Removes a project and returns the project name instance of the removed project.
        /// </summary>
        /// <param name="name">name of the project to remove.</param>
        public void RemoveProject(string name)
        {
            ProjectName projectName;
            if (_projectNamesCache.TryGetValue(name, out projectName))
            {
                // Remove from both caches
                RemoveProjectName(projectName);
                RemoveShortName(projectName);
            }
        }

        public bool Contains(string name)
        {
            if (name == null)
            {
                return false;
            }


            return _projectNamesCache.ContainsKey(name) ||
                   _shortNameCache.ContainsKey(name);
        }

        /// <summary>
        /// Returns all cached projects.
        /// </summary>
        public IEnumerable<Project> GetProjects()
        {
            return _projectCache.Values;
        }

        /// <summary>
        /// Determines if a short name is ambiguous
        /// </summary>
        /// <param name="shortName">short name of the project</param>
        /// <returns>true if there are multiple projects with the specified short name.</returns>
        public bool IsAmbiguous(string shortName)
        {
            HashSet<ProjectName> projectNames;
            if (_shortNameCache.TryGetValue(shortName, out projectNames))
            {
                return projectNames.Count > 1;
            }
            return false;
        }

        /// <summary>
        /// Add a project to the cache.
        /// </summary>
        /// <param name="project">project to add to the cache.</param>
        /// <returns>The project name of the added project.</returns>
        public ProjectName AddProject(Project project)
        {
            // First create a project name from the project
            var projectName = new ProjectName(project);

            // Do nothing if we already have an entry
            if (_projectCache.ContainsKey(projectName))
            {
                return projectName;
            }

            AddShortName(projectName);

            _projectNamesCache[projectName.CustomUniqueName] = projectName;
            _projectNamesCache[projectName.UniqueName] = projectName;
            _projectNamesCache[projectName.FullName] = projectName;

            // Add the entry mapping project name to the actual project
            _projectCache[projectName] = project;

            return projectName;
        }

        /// <summary>
        /// Tries to find a project by short name. Returns the project name if and only if it is non-ambiguous.
        /// </summary>
        public bool TryGetProjectNameByShortName(string name, out ProjectName projectName)
        {
            projectName = null;

            HashSet<ProjectName> projectNames;
            if (_shortNameCache.TryGetValue(name, out projectNames))
            {
                // Get the item at the front of the queue
                projectName = projectNames.Count == 1 ? projectNames.Single() : null;

                // Only return true if the short name is unambiguous
                return projectName != null;
            }

            return false;
        }

        /// <summary>
        /// Adds an entry to the short name cache returning any conflicting project name.
        /// </summary>
        /// <returns>The first conflicting short name.</returns>
        private void AddShortName(ProjectName projectName)
        {
            HashSet<ProjectName> projectNames;
            if (!_shortNameCache.TryGetValue(projectName.ShortName, out projectNames))
            {
                projectNames = new HashSet<ProjectName>();
                _shortNameCache.Add(projectName.ShortName, projectNames);
            }

            projectNames.Add(projectName);
        }

        /// <summary>
        /// Removes a project from the short name cache.
        /// </summary>
        /// <param name="projectName">The short name of the project.</param>
        private void RemoveShortName(ProjectName projectName)
        {
            HashSet<ProjectName> projectNames;
            if (_shortNameCache.TryGetValue(projectName.ShortName, out projectNames))
            {
                projectNames.Remove(projectName);

                // Remove the item from the dictionary if we've removed the last project
                if (projectNames.Count == 0)
                {
                    _shortNameCache.Remove(projectName.ShortName);
                }
            }
        }

        /// <summary>
        /// Removes a project from the project name dictionary.
        /// </summary>
        private void RemoveProjectName(ProjectName projectName)
        {
            _projectNamesCache.Remove(projectName.CustomUniqueName);
            _projectNamesCache.Remove(projectName.UniqueName);
            _projectNamesCache.Remove(projectName.FullName);
            _projectCache.Remove(projectName);
        }
    }
}

using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public interface ISolutionManager
    {
        event EventHandler SolutionOpened;
        event EventHandler SolutionClosing;
        event EventHandler SolutionClosed;
        event EventHandler<ProjectEventArgs> ProjectAdded;

        string SolutionDirectory { get; }
        IFileSystem SolutionFileSystem { get; }

        string DefaultProjectName { get; set; }
        Project DefaultProject { get; }

        Project GetProject(string projectSafeName);

        IEnumerable<Project> GetProjects();

        /// <summary>
        /// Get the safe name of the specified project which guarantees not to conflict with other projects.
        /// </summary>
        /// <remarks>
        /// It tries to return simple name if possible. Otherwise it returns the unique name.
        /// </remarks>
        string GetProjectSafeName(Project project);

        IEnumerable<Project> GetDependentProjects(Project project);

        bool IsSolutionOpen { get; }

        bool IsSourceControlBound { get; }
    }
}
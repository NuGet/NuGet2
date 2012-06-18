using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.VisualStudio.Test.Mocks
{
    public abstract class MockSolutionManager : ISolutionManager
    {
        public event EventHandler SolutionOpened;

        public event EventHandler SolutionClosing;

        public event EventHandler SolutionClosed;

        public event EventHandler<ProjectEventArgs> ProjectAdded;

        public abstract string SolutionDirectory
        {
            get;
        }

        public abstract IFileSystem SolutionFileSystem
        {
            get;
        }

        public abstract bool IsSolutionOpen { get; }

        public string DefaultProjectName
        {
            get;
            set;
        }

        public abstract Project DefaultProject
        {
            get;
        }

        public abstract Project GetProject(string projectSafeName);

        public abstract IEnumerable<Project> GetProjects();

        public abstract string GetProjectSafeName(Project project);

        public void CloseSolution()
        {
            if (SolutionClosing != null)
            {
                SolutionClosing(this, EventArgs.Empty);
            }

            if (SolutionClosed != null)
            {
                SolutionClosed(this, EventArgs.Empty);
            }
        }

        public void OpenSolution()
        {
            if (SolutionOpened != null)
            {
                SolutionOpened(this, EventArgs.Empty);
            }
        }

        public void AddProject(Project project)
        {
            if (ProjectAdded != null)
            {
                ProjectAdded(this, new ProjectEventArgs(project));
            }
        }

        public IEnumerable<Project> GetDependentProjects(Project project)
        {
            throw new NotImplementedException();
        }

        public bool IsSourceControlBound
        {
            get { return false; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Moq;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;

namespace NuGet.VisualStudio.Test
{
    public class MockVsPackageManager2 : VsPackageManager
    {
        // List of projects where binding redirection is performed.
        public IList<string> BindingRedirectedProjects 
        {
            get; private set;
        }

        private Dictionary<Project, IProjectManager> _projectManagers;

        public MockVsPackageManager2(
            string solutionDirectory,
            IPackageRepository sourceRepository):
            base(
            new MockSolutionManager2(solutionDirectory), 
            sourceRepository, 
            new MockFileSystemProvider(),
            new MockFileSystem(solutionDirectory),
            new MockSharedPackageRepository2(),
            new Mock<IDeleteOnRestartManager>().Object,
            new VsPackageInstallerEvents())
        {
            BindingRedirectedProjects = new List<string>();
            _projectManagers = new Dictionary<Project, IProjectManager>();
            AddProject("default");
        }

        public override IProjectManager GetProjectManager(Project project)
        {
            IProjectManager projectManager;
            if (_projectManagers.TryGetValue(project, out projectManager))
            {
                return projectManager;
            }

            var projectSystem = new MockVsProjectSystem(project);
            var localRepository = new PackageReferenceRepository(
                new MockFileSystem(project.GetFullPath()),
                project.Name,
                LocalRepository);
            projectManager = new ProjectManager(
                this,
                PathResolver, 
                projectSystem,
                localRepository);
            _projectManagers[project] = projectManager;
            return projectManager;
        }

        public void AddProject(string projectName)
        {
            var projectDirectory = Path.Combine(SolutionManager.SolutionDirectory, projectName);

            var sm = (MockSolutionManager2)SolutionManager;
            sm.AddProject(projectDirectory);
        }

        public override void AddBindingRedirects(IProjectManager projectManager)
        {
            BindingRedirectedProjects.Add(projectManager.Project.ProjectName);
        }
    }

    public class MockVsProjectSystem : MockProjectSystem, IVsProjectSystem
    {
        Project _project;

        public MockVsProjectSystem(Project project) 
            : base(VersionUtility.DefaultTargetFramework, project.GetFullPath())
        {
            _project = project;
        }

        public string UniqueName
        {
            get { return _project.GetUniqueName(); }
        }
    }

    public class MockSolutionManager2 : ISolutionManager
    {
        // Disable warnings that those events are never used since this is intentional for
        // this mock object.
#pragma warning disable 0067
        public event EventHandler SolutionOpened;
        public event EventHandler SolutionClosing;
        public event EventHandler SolutionClosed;
        public event EventHandler<ProjectEventArgs> ProjectAdded;
#pragma warning restore 0067

        Dictionary<string, Project> _projects;
        
        public MockSolutionManager2(string solutionDirectory)
        {
            SolutionDirectory = solutionDirectory;
            SolutionFileSystem = new MockFileSystem(solutionDirectory);
            _projects = new Dictionary<string, Project>();
            IsSolutionOpen = true;
        }

        public string SolutionDirectory
        {
            get;
            private set;
        }

        public IFileSystem SolutionFileSystem
        {
            get;
            private set;
        }

        public string DefaultProjectName
        {
            get;
            set;
        }

        public Project DefaultProject
        {
            get
            {
                Project project;
                if (_projects.TryGetValue(DefaultProjectName, out project))
                {
                    return project;
                }
                else
                {
                    return null;
                }
            }
        }

        public Project GetProject(string projectSafeName)
        {
            Project project;
            if (_projects.TryGetValue(projectSafeName, out project))
            {
                return project;
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<Project> GetProjects()
        {
            return _projects.Values;
        }

        public string GetProjectSafeName(Project project)
        {
            return project.Name;
        }

        public IEnumerable<Project> GetDependentProjects(Project project)
        {
            return Enumerable.Empty<Project>();
        }

        public bool IsSolutionOpen
        {
            get;
            set;
        }

        public bool IsSourceControlBound
        {
            get { return false; }
        }

        internal void AddProject(string projectDirectory)
        {
            var project = TestUtils.GetProject(projectDirectory);
            _projects[project.Name] = project;
            DefaultProjectName = project.Name;
        }
    }

    public class MockFileSystemProvider : IFileSystemProvider
    {
        public IFileSystem GetFileSystem(string path)
        {
            return new MockFileSystem(path);
        }

        public IFileSystem GetFileSystem(string path, bool ignoreSourceControlSetting)
        {
            return new MockFileSystem(path);
        }
    }
}

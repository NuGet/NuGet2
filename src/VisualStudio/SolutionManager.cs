using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ISolutionManager))]
    public class SolutionManager : ISolutionManager, IVsSelectionEvents
    {
        private readonly DTE _dte;
        private readonly SolutionEvents _solutionEvents;
        private readonly IVsMonitorSelection _vsMonitorSelection;
        private readonly uint _solutionLoadedUICookie;
        private readonly IVsSolution _vsSolution;

        private ProjectCache _projectCache;

        public SolutionManager()
            : this(
                ServiceLocator.GetInstance<DTE>(), 
                ServiceLocator.GetGlobalService<SVsSolution, IVsSolution>(),
                ServiceLocator.GetGlobalService<SVsShellMonitorSelection, IVsMonitorSelection>())
        {
        }

        internal SolutionManager(DTE dte, IVsSolution vsSolution, IVsMonitorSelection vsMonitorSelection)
        {
            if (dte == null)
            {
                throw new ArgumentNullException("dte");
            }

            _dte = dte;
            _vsSolution = vsSolution;
            _vsMonitorSelection = vsMonitorSelection;

            // Keep a reference to SolutionEvents so that it doesn't get GC'ed. Otherwise, we won't receive events.
            _solutionEvents = _dte.Events.SolutionEvents;

            // can be null in unit tests
            if (vsMonitorSelection != null)
            {
                Guid solutionLoadedGuid = VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid;
                _vsMonitorSelection.GetCmdUIContextCookie(ref solutionLoadedGuid, out _solutionLoadedUICookie);

                uint cookie;
                int hr = _vsMonitorSelection.AdviseSelectionEvents(this, out cookie);
                ErrorHandler.ThrowOnFailure(hr);
            }
            
            _solutionEvents.BeforeClosing += OnBeforeClosing;
            _solutionEvents.AfterClosing += OnAfterClosing;
            _solutionEvents.ProjectAdded += OnProjectAdded;
            _solutionEvents.ProjectRemoved += OnProjectRemoved;
            _solutionEvents.ProjectRenamed += OnProjectRenamed;

            if (_dte.Solution.IsOpen)
            {
                OnSolutionOpened();
            }
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
                if (String.IsNullOrEmpty(DefaultProjectName))
                {
                    return null;
                }
                Project project = GetProject(DefaultProjectName);
                Debug.Assert(project != null, "Invalid default project");
                return project;
            }
        }

        [Import]
        internal Lazy<IDeleteOnRestartManager> DeleteOnRestartManager { get; set; }

        public event EventHandler SolutionOpened;

        public event EventHandler SolutionClosing;

        public event EventHandler SolutionClosed;

        public event EventHandler<ProjectEventArgs> ProjectAdded;

        /// <summary>
        /// Gets a value indicating whether there is a solution open in the IDE.
        /// </summary>
        public bool IsSolutionOpen
        {
            get
            {
                return _dte != null && 
                       _dte.Solution != null && 
                       _dte.Solution.IsOpen &&
                       !IsSolutionSavedAsRequired();
            }
        }

        /// <summary>
        /// Checks whether the current solution is saved to disk, as opposed to be in memory.
        /// </summary>
        private bool IsSolutionSavedAsRequired()
        {
            // Check if user is doing File - New File without saving the solution.
            object value;
            _vsSolution.GetProperty((int)(__VSPROPID.VSPROPID_IsSolutionSaveAsRequired), out value);
            if ((bool)value)
            {
                return true;
            }

            // Check if user unchecks the "Tools - Options - Project & Soltuions - Save new projects when created" option
            _vsSolution.GetProperty((int)(__VSPROPID2.VSPROPID_DeferredSaveSolution), out value);
            return (bool)value;
        }

        public string SolutionDirectory
        {
            get
            {
                if (!IsSolutionOpen)
                {
                    return null;
                }

                string solutionFilePath = GetSolutionFilePath();

                if (String.IsNullOrEmpty(solutionFilePath))
                {
                    return null;
                }
                return Path.GetDirectoryName(solutionFilePath);
            }
        }

        public IFileSystem SolutionFileSystem
        {
            get
            {
                string path = SolutionDirectory;
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }
                return new PhysicalFileSystem(path);
            }
        }

        private string GetSolutionFilePath()
        {
            // Use .Properties.Item("Path") instead of .FullName because .FullName might not be
            // available if the solution is just being created
            string solutionFilePath = null;

            Property property = _dte.Solution.Properties.Item("Path");
            if (property == null)
            {
                return null;
            }
            try
            {
                // When using a temporary solution, (such as by saying File -> New File), querying this value throws.
                // Since we wouldn't be able to do manage any packages at this point, we return null. Consumers of this property typically 
                // use a String.IsNullOrEmpty check either way, so it's alright.
                solutionFilePath = (string)property.Value;
            }
            catch (COMException)
            {
                return null;
            }

            return solutionFilePath;
        }

        public bool IsSourceControlBound
        {
            get
            {
                return GetIsSourceControlBound();
            }
        }

        private bool GetIsSourceControlBound()
        {
            if (!IsSolutionOpen)
            {
                return false;
            }

            string solutionFilePath = GetSolutionFilePath();
            Debug.Assert(!String.IsNullOrEmpty(solutionFilePath));

            SourceControl2 sourceControl = (SourceControl2)_dte.SourceControl;
            if (sourceControl != null)
            {
                try
                {
                    return sourceControl.GetBindings(solutionFilePath) != null;
                }
                catch (NotImplementedException)
                {
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a list of supported projects currently loaded in the solution
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This method is potentially expensive if the cache is not constructed yet.")]
        public IEnumerable<Project> GetProjects()
        {
            if (IsSolutionOpen)
            {
                EnsureProjectCache();
                return _projectCache.GetProjects();
            }
            else
            {
                return Enumerable.Empty<Project>();
            }
        }

        public string GetProjectSafeName(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            // Try searching for simple names first
            string name = project.GetName();
            if (GetProject(name) == project)
            {
                return name;
            }

            return project.GetCustomUniqueName();
        }

        public Project GetProject(string projectSafeName)
        {
            if (IsSolutionOpen)
            {
                EnsureProjectCache();

                Project project;
                if (_projectCache.TryGetProject(projectSafeName, out project))
                {
                    return project;
                }
            }

            return null;
        }

        private void OnAfterClosing()
        {
            if (SolutionClosed != null)
            {
                SolutionClosed(this, EventArgs.Empty);
            }
        }

        private void OnBeforeClosing()
        {
            DefaultProjectName = null;
            _projectCache = null;
            if (SolutionClosing != null)
            {
                SolutionClosing(this, EventArgs.Empty);
            }
        }

        private void OnProjectRenamed(Project project, string oldName)
        {
            if (!String.IsNullOrEmpty(oldName))
            {
                EnsureProjectCache();

                if (project.IsSupported())
                {
                    RemoveProjectFromCache(oldName);
                    AddProjectToCache(project);
                }
                else if (project.IsSolutionFolder())
                {
                    // In the case where a solution directory was changed, project FullNames are unchanged. 
                    // We only need to invalidate the projects under the current tree so as to sync the CustomUniqueNames.
                    foreach (var item in project.GetSupportedChildProjects())
                    {
                        RemoveProjectFromCache(item.FullName);
                        AddProjectToCache(item);
                    }
                }
            }
        }

        private void OnProjectRemoved(Project project)
        {
            RemoveProjectFromCache(project.FullName);
        }

        private void OnProjectAdded(Project project)
        {
            if (project.IsSupported() && !project.IsParentProjectExplicitlyUnsupported())
            {
                EnsureProjectCache();
                AddProjectToCache(project);

                if (ProjectAdded != null)
                {
                    ProjectAdded(this, new ProjectEventArgs(project));
                }
            }
        }

        private void OnSolutionOpened()
        {
            // although the SolutionOpened event fires, the solution may be only in memory (e.g. when
            // doing File - New File). In that case, we don't want to act on the event.
            if (!IsSolutionOpen)
            {
                return;
            }

            EnsureProjectCache();
            SetDefaultProject();
            if (SolutionOpened != null)
            {
                SolutionOpened(this, EventArgs.Empty);
            }
        }

        private void EnsureProjectCache()
        {
            if (IsSolutionOpen && _projectCache == null)
            {
                _projectCache = new ProjectCache();

                var allProjects = _dte.Solution.GetAllProjects();
                foreach (Project project in allProjects)
                {
                    AddProjectToCache(project);
                }
            }
        }

        private void AddProjectToCache(Project project)
        {
            if (!project.IsSupported())
            {
                return;
            }
            ProjectName oldProjectName;
            _projectCache.TryGetProjectNameByShortName(project.GetName(), out oldProjectName);

            ProjectName newProjectName = _projectCache.AddProject(project);

            if (String.IsNullOrEmpty(DefaultProjectName) ||
                newProjectName.ShortName.Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase))
            {
                DefaultProjectName = oldProjectName != null ?
                                     oldProjectName.CustomUniqueName :
                                     newProjectName.ShortName;
            }
        }

        private void RemoveProjectFromCache(string name)
        {
            // Do nothing if the cache hasn't been set up
            if (_projectCache == null)
            {
                return;
            }

            ProjectName projectName;
            _projectCache.TryGetProjectName(name, out projectName);

            // Remove the project from the cache
            _projectCache.RemoveProject(name);

            if (!_projectCache.Contains(DefaultProjectName))
            {
                DefaultProjectName = null;
            }

            // for LightSwitch project, the main project is not added to _projectCache, but it is called on removal. 
            // in that case, projectName is null.
            if (projectName != null &&
                projectName.CustomUniqueName.Equals(DefaultProjectName, StringComparison.OrdinalIgnoreCase) &&
                !_projectCache.IsAmbiguous(projectName.ShortName))
            {
                DefaultProjectName = projectName.ShortName;
            }
        }

        private void SetDefaultProject()
        {
            // when a new solution opens, we set its startup project as the default project in NuGet Console
            var solutionBuild = (SolutionBuild2)_dte.Solution.SolutionBuild;
            if (solutionBuild.StartupProjects != null)
            {
                IEnumerable<object> startupProjects = (IEnumerable<object>)solutionBuild.StartupProjects;
                string startupProjectName = startupProjects.Cast<string>().FirstOrDefault();
                if (!String.IsNullOrEmpty(startupProjectName))
                {
                    ProjectName projectName;
                    if (_projectCache.TryGetProjectName(startupProjectName, out projectName))
                    {
                        DefaultProjectName = _projectCache.IsAmbiguous(projectName.ShortName) ?
                                             projectName.CustomUniqueName :
                                             projectName.ShortName;
                    }
                }
            }
        }

        // REVIEW: This might be inefficient, see what we can do with caching projects until references change
        public IEnumerable<Project> GetDependentProjects(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            var dependentProjects = new Dictionary<string, List<Project>>();

            // Get all of the projects in the solution and build the reverse graph. i.e.
            // if A has a project reference to B (A -> B) the this will return B -> A
            // We need to run this on the ui thread so that it doesn't freeze for websites. Since there might be a 
            // large number of references.           
            ThreadHelper.Generic.Invoke(() =>
            {
                foreach (var proj in GetProjects())
                {
                    if (proj.SupportsReferences())
                    {
                        foreach (var referencedProject in proj.GetReferencedProjects())
                        {
                            AddDependentProject(dependentProjects, referencedProject, proj);
                        }
                    }
                }
            });

            List<Project> dependents;
            if (dependentProjects.TryGetValue(project.GetUniqueName(), out dependents))
            {
                return dependents;
            }

            return Enumerable.Empty<Project>();
        }

        private static void AddDependentProject(IDictionary<string, List<Project>> dependentProjects, Project project, Project dependent)
        {
            string uniqueName = project.GetUniqueName();

            List<Project> dependents;
            if (!dependentProjects.TryGetValue(uniqueName, out dependents))
            {
                dependents = new List<Project>();
                dependentProjects[uniqueName] = dependents;
            }
            dependents.Add(dependent);
        }

        #region IVsSelectionEvents implementation

        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            if (dwCmdUICookie == _solutionLoadedUICookie && fActive == 1)
            {
                OnSolutionOpened();
                // We must call DeleteMarkedPackageDirectories outside of OnSolutionOpened, because OnSolutionOpened might be called in the constructor
                // and DeleteOnRestartManager requires VsFileSystemProvider and RepositorySetings which both have dependencies on SolutionManager.
                // In practice, this code gets executed even when a solution is opened directly during Visual Studio startup.
                DeleteOnRestartManager.Value.DeleteMarkedPackageDirectories();
            }

            return VSConstants.S_OK;
        }

        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            return VSConstants.S_OK;
        }

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        #endregion 
    }
}
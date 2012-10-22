using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio
{
    [Export(typeof(IVsSourceControlTracker))]
    internal class VsSourceControlTracker : IVsSourceControlTracker
    {
        private readonly TrackProjectDocumentEventListener _projectDocumentListener;
        private readonly IVsTrackProjectDocuments2 _projectTracker;
        private readonly ISolutionManager _solutionManager;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly ISettings _solutionSettings;
        private uint? _trackingCookie;

        [ImportingConstructor]
        public VsSourceControlTracker(ISolutionManager solutionManager, IFileSystemProvider fileSystemProvider, ISettings solutionSettings) :
            this(solutionManager, fileSystemProvider, ServiceLocator.GetGlobalService<SVsTrackProjectDocuments, IVsTrackProjectDocuments2>(), solutionSettings)
        {
        }

        public VsSourceControlTracker(
            ISolutionManager solutionManager, 
            IFileSystemProvider fileSystemProvider,
            IVsTrackProjectDocuments2 projectTracker,
            ISettings solutionSettings)
        {
            if (projectTracker == null)
            {
                throw new ArgumentNullException("projectTracker");
            }

            _solutionManager = solutionManager;
            _projectTracker = projectTracker;
            _fileSystemProvider = fileSystemProvider;
            _solutionSettings = solutionSettings;
            _projectDocumentListener = new TrackProjectDocumentEventListener(this);

            _solutionManager.SolutionOpened += OnSolutionOpened;
            _solutionManager.SolutionClosed += OnSolutionClosed;

            if (_solutionManager.IsSolutionOpen)
            {
                StartTracking();
            }
        }

        [Import]
        public Lazy<IRepositorySettings> RepositorySettings { get; set; }

        public event EventHandler SolutionBoundToSourceControl = delegate { };

        private void OnSolutionOpened(object sender, EventArgs e)
        {
            StartTracking();
        }

        private void OnSolutionClosed(object sender, EventArgs e)
        {
            StopTracking();
        }

        private bool IsTracking
        {
            get
            {
                return _trackingCookie != null;
            }
        }

        private void StartTracking()
        {
            // don't track again if already tracking
            if (IsTracking)
            {
                return;
            }

            // don't do anything if user explicitly disables source control integration
            if (_solutionSettings.IsSourceControlDisabled())
            {
                return;
            }

            uint cookie;
            _projectTracker.AdviseTrackProjectDocumentsEvents(_projectDocumentListener, out cookie);
            _trackingCookie = cookie;
        }

        private void StopTracking()
        {
            if (!IsTracking)
            {
                return;
            }

            _projectTracker.UnadviseTrackProjectDocumentsEvents((uint)_trackingCookie);
            _trackingCookie = null;
        }

        private void OnSourceControlBound()
        {
            Debug.Assert(RepositorySettings.Value != null);

            if (RepositorySettings.Value == null)
            {
                return;
            }

            try
            {
                string solutionRepositoryPath = RepositorySettings.Value.RepositoryPath;
                if (Directory.Exists(solutionRepositoryPath))
                {
                    
                    var activeFileSystem = _fileSystemProvider.GetFileSystem(solutionRepositoryPath) as ISourceControlFileSystem;

                    // only proceed if the file system in use is a source-control file system.
                    if (activeFileSystem != null)
                    {
                        IEnumerable<string> allFiles = Directory.EnumerateFiles(solutionRepositoryPath, "*.*",
                                                                                SearchOption.AllDirectories);
                        string file = allFiles.FirstOrDefault();
                        if (file != null && !activeFileSystem.IsSourceControlBound(file))
                        {
                            // If there are any files under the packages directory and any given file (in this case the first one we come across) is not under
                            // source control, bind it.
                            activeFileSystem.BindToSourceControl(allFiles);
                        }
                    }
                }
            }
            finally
            {
                StopTracking();

                // raise event
                SolutionBoundToSourceControl(this, EventArgs.Empty);
            }
        }

        private class TrackProjectDocumentEventListener : IVsTrackProjectDocumentsEvents2
        {
            private readonly VsSourceControlTracker _parent;

            public TrackProjectDocumentEventListener(VsSourceControlTracker parent)
            {
                _parent = parent;
            }

            public int OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus)
            {
                // The value 0x1F is the AND of the following bits.
                // We want to check if any of these 5 bits is on. 

                // SCC_STATUS_CONTROLLED = 1,
                // SCC_STATUS_CHECKEDOUT = 2,
                // SCC_STATUS_OUTOTHER = 4,
                // SCC_STATUS_OUTEXCLUSIVE = 8,
                // SCC_STATUS_OUTMULTIPLE = 16,

                if (cProjects > 0 && 
                    cFiles > 0 && 
                    rgdwSccStatus != null &&
                    rgdwSccStatus.Any(f => (f & 0x1F) != 0) &&
                    rgpszMkDocuments != null &&
                    rgpszMkDocuments.Any(s => s.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)))
                {
                    _parent.OnSourceControlBound();
                }

                return VsConstants.S_OK;
            }

            #region Irrelevant members

            public int OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
            {
                return VsConstants.S_OK;
            }

            public int OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
            {
                return VsConstants.S_OK;
            }

            public int OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
            {
                return VsConstants.S_OK;
            }

            public int OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
            {
                return VsConstants.S_OK;
            }

            public int OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
            {
                return VsConstants.S_OK;
            }

            public int OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
            {
                return VsConstants.S_OK;
            }

            public int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults)
            {
                return VsConstants.S_OK;
            }

            public int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
            {
                return VsConstants.S_OK;
            }

            public int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
            {
                return VsConstants.S_OK;
            }

            public int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
            {
                return VsConstants.S_OK;
            }

            public int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
            {
                return VsConstants.S_OK;
            }

            public int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
            {
                return VsConstants.S_OK;
            }

            #endregion
        }
    }
}
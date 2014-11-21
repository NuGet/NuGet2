using Microsoft.VisualStudio.Shell;
using NuGet.Client;
using NuGet.Client.ProjectSystem;
using NuGet.Client.VisualStudio;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;



#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
#endif

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This command process the specified package against the specified project.
    /// </summary>
    public abstract class NuGetPowerShellBaseCommand : PSCmdlet, IExecutionLogger, IErrorHandler
    {
        private VsSourceRepositoryManager _repoManager;
        private IVsPackageSourceProvider _packageSourceProvider;
        private IPackageRepositoryFactory _repositoryFactory;
        private SVsServiceProvider _serviceProvider;
        private IVsPackageManagerFactory _packageManagerFactory;
        private ISolutionManager _solutionManager;
        private VsPackageManagerContext _VsContext;
        private readonly IHttpClientEvents _httpClientEvents;
        internal const string PSCommandsUserAgentClient = "NuGet VS PowerShell Console";
        private readonly Lazy<string> _psCommandsUserAgent = new Lazy<string>(
            () => HttpUtility.CreateUserAgentString(PSCommandsUserAgentClient, VsVersionHelper.FullVsEdition));
        private ProgressRecordCollection _progressRecordCache;
        private VsSolution _solution;
        private string _projectName;

        public NuGetPowerShellBaseCommand(
            IVsPackageSourceProvider psProvider,
            IPackageRepositoryFactory prFactory,
            SVsServiceProvider svcProvider,
            IVsPackageManagerFactory pmFactory,
            IHttpClientEvents clientEvents)
        {
            _packageSourceProvider = psProvider;
            _repositoryFactory = prFactory;
            _serviceProvider = svcProvider;
            _packageManagerFactory = pmFactory;
            _solutionManager = new SolutionManager();
            _repoManager = new VsSourceRepositoryManager(_packageSourceProvider, _repositoryFactory);
            _VsContext = new VsPackageManagerContext(_repoManager, _serviceProvider, _solutionManager, _packageManagerFactory);
            _solution = _VsContext.GetCurrentVsSolution();
            _httpClientEvents = clientEvents;
        }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        public virtual string ProjectName
        {
            get
            {
                if (String.IsNullOrEmpty(_projectName))
                {
                    _projectName = _solutionManager.DefaultProjectName;
                }
                return _projectName;
            }
            set
            {
                _projectName = value;
            }
        }

        internal VsSolution Solution
        {
            get
            {
                return _solution;
            }
            set
            {
                _solution = value;
            }
        }

        internal VsSourceRepositoryManager RepositoryManager
        {
            get
            {
                return _repoManager;
            }
            set
            {
                _repoManager = value;
            }
        }

        internal IVsPackageManagerFactory PackageManagerFactory
        {
            get
            {
                return _packageManagerFactory;
            }
            set
            {
                _packageManagerFactory = value;
            }
        }

        internal ISolutionManager SolutionManager
        {
            get
            {
                return _solutionManager;
            }
        }

        internal bool IsSyncMode
        {
            get
            {
                if (Host == null || Host.PrivateData == null)
                {
                    return false;
                }

                PSObject privateData = Host.PrivateData;
                var syncModeProp = privateData.Properties["IsSyncMode"];
                return syncModeProp != null && (bool)syncModeProp.Value;
            }
        }

        internal void Execute()
        {
            BeginProcessing();
            ProcessRecord();
            EndProcessing();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to display friendly message to the console.")]
        protected sealed override void ProcessRecord()
        {
            try
            {
                ProcessRecordCore();
            }
            catch (Exception ex)
            {
                // unhandled exceptions should be terminating
                ErrorHandler.HandleException(ex, terminating: true);
            }
            finally
            {
                UnsubscribeEvents();
            }
        }

        /// <summary>
        /// Derived classess must implement this method instead of ProcessRecord(), which is sealed by NuGetBaseCmdlet.
        /// </summary>
        protected abstract void ProcessRecordCore();

        protected override void BeginProcessing()
        {
            if (_httpClientEvents != null)
            {
                _httpClientEvents.SendingRequest += OnSendingRequest;
            }
        }

        protected override void StopProcessing()
        {
            UnsubscribeEvents();
            base.StopProcessing();
        }

        protected void UnsubscribeEvents()
        {
            if (_httpClientEvents != null)
            {
                _httpClientEvents.SendingRequest -= OnSendingRequest;
            }
        }

        protected virtual void OnSendingRequest(object sender, WebRequestEventArgs e)
        {
            HttpUtility.SetUserAgent(e.Request, _psCommandsUserAgent.Value);
        }

        protected IErrorHandler ErrorHandler
        {
            get
            {
                return this;
            }
        }

        #region Logging
        public void Log(Client.MessageLevel level, string message, params object[] args)
        {
            string formattedMessage = String.Format(CultureInfo.CurrentCulture, message, args);
            LogCore(level, formattedMessage);
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            return FileConflictAction.IgnoreAll;
        }

        void IErrorHandler.HandleError(ErrorRecord errorRecord, bool terminating)
        {
            if (terminating)
            {
                ThrowTerminatingError(errorRecord);
            }
            else
            {
                WriteError(errorRecord);
            }
        }

        void IErrorHandler.HandleException(Exception exception, bool terminating,
            string errorId, ErrorCategory category, object target)
        {

            exception = ExceptionUtility.Unwrap(exception);

            var error = new ErrorRecord(exception, errorId, category, target);

            ErrorHandler.HandleError(error, terminating: terminating);
        }

        protected void WriteLine(string message = null)
        {
            if (Host == null)
            {
                // Host is null when running unit tests. Simply return in this case
                return;
            }

            if (message == null)
            {
                Host.UI.WriteLine();
            }
            else
            {
                Host.UI.WriteLine(message);
            }
        }

        protected void WriteProgress(int activityId, string operation, int percentComplete)
        {
            if (IsSyncMode)
            {
                // don't bother to show progress if we are in synchronous mode
                return;
            }

            ProgressRecord progressRecord;

            // retrieve the ProgressRecord object for this particular activity id from the cache.
            if (ProgressRecordCache.Contains(activityId))
            {
                progressRecord = ProgressRecordCache[activityId];
            }
            else
            {
                progressRecord = new ProgressRecord(activityId, operation, operation);
                ProgressRecordCache.Add(progressRecord);
            }

            progressRecord.CurrentOperation = operation;
            progressRecord.PercentComplete = percentComplete;

            WriteProgress(progressRecord);
        }

        private void OnProgressAvailable(object sender, ProgressEventArgs e)
        {
            WriteProgress(ProgressActivityIds.DownloadPackageId, e.Operation, e.PercentComplete);
        }

        protected void SubscribeToProgressEvents()
        {
            if (!IsSyncMode && _httpClientEvents != null)
            {
                _httpClientEvents.ProgressAvailable += OnProgressAvailable;
            }
        }

        protected void UnsubscribeFromProgressEvents()
        {
            if (_httpClientEvents != null)
            {
                _httpClientEvents.ProgressAvailable -= OnProgressAvailable;
            }
        }

        protected virtual void LogCore(Client.MessageLevel level, string formattedMessage)
        {
            try
            {
                switch (level)
                {
                    case Client.MessageLevel.Debug:
                        WriteVerbose(formattedMessage);
                        break;

                    case Client.MessageLevel.Warning:
                        WriteWarning(formattedMessage);
                        break;

                    case Client.MessageLevel.Info:
                        WriteLine(formattedMessage);
                        break;

                    case Client.MessageLevel.Error:
                        WriteError(formattedMessage);
                        break;
                }
            }
            catch (PSInvalidOperationException) { }
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "This exception is passed to PowerShell. We really don't care about the type of exception here.")]
        protected void WriteError(string message)
        {
            if (!String.IsNullOrEmpty(message))
            {
                WriteError(new Exception(message));
            }
        }

        protected void WriteError(Exception exception)
        {
            ErrorHandler.HandleException(exception, terminating: false);
        }

        private ProgressRecordCollection ProgressRecordCache
        {
            get
            {
                if (_progressRecordCache == null)
                {
                    _progressRecordCache = new ProgressRecordCollection();
                }

                return _progressRecordCache;
            }
        }

        void IErrorHandler.WriteProjectNotFoundError(string projectName, bool terminating)
        {
            var notFoundException =
                new ItemNotFoundException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Cmdlet_ProjectNotFound, projectName));

            ErrorHandler.HandleError(
                new ErrorRecord(
                    notFoundException,
                    NuGetErrorId.ProjectNotFound, // This is your locale-agnostic error id.
                    ErrorCategory.ObjectNotFound,
                    projectName),
                    terminating: terminating);
        }

        void IErrorHandler.ThrowSolutionNotOpenTerminatingError()
        {
            ErrorHandler.HandleException(
                new InvalidOperationException(Resources.Cmdlet_NoSolution),
                terminating: true,
                errorId: NuGetErrorId.NoActiveSolution,
                category: ErrorCategory.InvalidOperation);
        }

        void IErrorHandler.ThrowNoCompatibleProjectsTerminatingError()
        {
            ErrorHandler.HandleException(
                new InvalidOperationException(Resources.Cmdlet_NoCompatibleProjects),
                terminating: true,
                errorId: NuGetErrorId.NoCompatibleProjects,
                category: ErrorCategory.InvalidOperation);
        }
        #endregion

        #region Project APIs
        public VsProject GetProject(bool throwIfNotExists)
        {
            VsProject project = null;

            // If the user specified a project then use it
            if (!String.IsNullOrEmpty(ProjectName))
            {
                EnvDTE.Project dteProject = Solution.DteSolution.GetAllProjects()
                    .FirstOrDefault(p => String.Equals(p.Name, ProjectName, StringComparison.OrdinalIgnoreCase));
                project = Solution.GetProject(dteProject);

                // If that project was invalid then throw
                if (project == null && throwIfNotExists)
                {
                    ErrorHandler.ThrowNoCompatibleProjectsTerminatingError();
                }
            }

            return project;
        }

        /// <summary>
        /// Return all projects in the solution matching the provided names. Wildcards are supported.
        /// This method will automatically generate error records for non-wildcarded project names that
        /// are not found.
        /// </summary>
        /// <param name="projectNames">An array of project names that may or may not include wildcards.</param>
        /// <returns>Projects matching the project name(s) provided.</returns>
        protected IEnumerable<Project> GetProjectsByName(string[] projectNames)
        {
            var allValidProjectNames = GetAllValidProjectNames().ToList();

            foreach (string projectName in projectNames)
            {
                // if ctrl+c hit, leave immediately
                if (Stopping)
                {
                    break;
                }

                // Treat every name as a wildcard; results in simpler code
                var pattern = new WildcardPattern(projectName, WildcardOptions.IgnoreCase);

                var matches = from s in allValidProjectNames
                              where pattern.IsMatch(s)
                              select _solutionManager.GetProject(s);

                int count = 0;
                foreach (var project in matches)
                {
                    count++;
                    VsProject proj = Solution.GetProject(project);
                    yield return proj;
                }

                // We only emit non-terminating error record if a non-wildcarded name was not found.
                // This is consistent with built-in cmdlets that support wildcarded search.
                // A search with a wildcard that returns nothing should not be considered an error.
                if ((count == 0) && !WildcardPattern.ContainsWildcardCharacters(projectName))
                {
                    ErrorHandler.WriteProjectNotFoundError(projectName, terminating: false);
                }
            }
        }

        /// <summary>
        /// Return all possibly valid project names in the current solution. This includes all 
        /// unique names and safe names.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetAllValidProjectNames()
        {
            var safeNames = _solutionManager.GetProjects().Select(p => _solutionManager.GetProjectSafeName(p));
            var uniqueNames = _solutionManager.GetProjects().Select(p => p.GetCustomUniqueName());
            return uniqueNames.Concat(safeNames).Distinct();
        }
        #endregion
    }

    public class ProgressRecordCollection : KeyedCollection<int, ProgressRecord>
    {
        protected override int GetKeyForItem(ProgressRecord item)
        {
            return item.ActivityId;
        }
    }
}
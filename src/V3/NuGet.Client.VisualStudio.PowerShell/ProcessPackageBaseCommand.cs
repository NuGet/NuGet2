using Microsoft.VisualStudio.Shell;
using NuGet.Client;
using NuGet.Client.Installation;
using NuGet.Client.Resolution;
using NuGet.Client.VisualStudio;
using NuGet.Client.VisualStudio.PowerShell;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using NuGet.PowerShell.Commands;


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
    public abstract class ProcessPackageBaseCommand : PSCmdlet, IExecutionLogger, IErrorHandler
    {
        private VsSourceRepositoryManager _repoManager;
        private IVsPackageSourceProvider _packageSourceProvider;
        private IPackageRepositoryFactory _repositoryFactory;
        private SVsServiceProvider _serviceProvider;
        private IVsPackageManagerFactory _packageManagerFactory;
        private ISolutionManager _solutionManager;
        private VsPackageManagerContext _VsContext;
        private PackageIdentity _identity;
        private readonly IHttpClientEvents _httpClientEvents;
        private const string PSCommandsUserAgentClient = "NuGet VS PowerShell Console";
        private readonly Lazy<string> _psCommandsUserAgent = new Lazy<string>(
            () => HttpUtility.CreateUserAgentString(PSCommandsUserAgentClient, VsVersionHelper.FullVsEdition));
        private ProgressRecordCollection _progressRecordCache;
        private VsSolution _solution;
        private string _projectName;
        private PackageActionType _actionType;
        private string _version;

        public ProcessPackageBaseCommand(IVsPackageSourceProvider psProvider, IPackageRepositoryFactory prFactory,
                      SVsServiceProvider svcProvider, IVsPackageManagerFactory pmFactory, IHttpClientEvents clientEvents, PackageActionType actionType)
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
            _actionType = actionType;
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public virtual string Id { get; set; }

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

        [Parameter(Position = 2)]
        public string Version
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                {
                    IsVersionSpecified = false;
                    _version = VersionUtil.GetLastestVersionForPackage(_repoManager.ActiveRepository, this.Id);
                }
                return _version;
            }
            set
            {
                IsVersionSpecified = true;
                _version = value;
            }
        }

        [Parameter(Position = 3)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }


        public PackageIdentity Identity
        {
            get
            {
                _identity = new PackageIdentity(Id, NuGetVersion.Parse(Version));
                return _identity;
            }
        }

        public ActionResolver PackageActionResolver { get; set; }

        public SourceRepository V3SourceRepository
        {
            get
            {
                return _repoManager.ActiveRepository;
            }
        }

        public IPackageRepository V2LocalRepository
        {
            get 
            {
                var packageManager = _packageManagerFactory.CreatePackageManager();
                return packageManager.LocalRepository;
            }
        }

        public IEnumerable<VsProject> TargetedProjects
        {
            get
            {
                EnvDTE.Project project = _solution.DteSolution.GetAllProjects()
                    .FirstOrDefault(p => String.Equals(p.Name, ProjectName, StringComparison.OrdinalIgnoreCase));
                VsProject vsProject = _solution.GetProject(project);
                List<VsProject> targetedProjects = new List<VsProject> { vsProject };
                return targetedProjects;
            }
        }

        public bool IsVersionSpecified { get; set; }

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
                CheckForSolutionOpen();
                ResolvePackageFromRepository();
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

        protected void CheckForSolutionOpen()
        {
            if (!_solutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }
        }

        protected virtual void ResolvePackageFromRepository()
        {
        }

        protected void ProcessRecordCore()
        {
            try
            {
                SubscribeToProgressEvents();

                // Resolve Actions
                var targetProjects = TargetedProjects.ToList();
                var resolverAction = PackageActionResolver.ResolveActionsAsync(
                    Identity, _actionType, targetProjects, _solution);
                var actions = resolverAction.Result;

                // Execute Actions
                ActionExecutor executor = new ActionExecutor();
                Task task = executor.ExecuteActionsAsync(actions, this, CancellationToken.None);
                task.Wait();
            }
            catch (Exception ex)
            {
                this.Log(Client.MessageLevel.Warning, ex.InnerException.Message);
            }
            finally
            {
                UnsubscribeFromProgressEvents();
            }
        }

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

        private void UnsubscribeEvents()
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
    }

    public class ProgressRecordCollection : KeyedCollection<int, ProgressRecord>
    {
        protected override int GetKeyForItem(ProgressRecord item)
        {
            return item.ActivityId;
        }
    }
}
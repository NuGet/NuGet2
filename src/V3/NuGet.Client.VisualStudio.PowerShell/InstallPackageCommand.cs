using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using NuGet.Client;
using NuGet.Client.Installation;
using NuGet.Client.Resolution;
using NuGet.Client.VisualStudio;
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


#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
#endif

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Package2")]
    public class InstallPackageCommand : PSCmdlet, IExecutionLogger, IErrorHandler
    {
        private ActionResolver _actionResolver;
        private VsSourceRepositoryManager _repoManager;
        private IVsPackageSourceProvider _packageSourceProvider;
        private IPackageRepositoryFactory _repositoryFactory;
        private SVsServiceProvider _serviceProvider;
        private IVsPackageManagerFactory _packageManagerFactory;
        private ISolutionManager _solutionManager;
        private VsPackageManagerContext _VsContext;
        private PackageIdentity _identity;
        private ResolutionContext _context;
        private readonly EnvDTE._DTE _dte;
        private readonly IHttpClientEvents _httpClientEvents;
        private const string PSCommandsUserAgentClient = "NuGet VS PowerShell Console";
        private readonly Lazy<string> _psCommandsUserAgent = new Lazy<string>(
            () => HttpUtility.CreateUserAgentString(PSCommandsUserAgentClient, VsVersionHelper.FullVsEdition));
        private ProgressRecordCollection _progressRecordCache;
        private VsSolution _solution;
        private string _projectName;
        private string _version;

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
                if (String.IsNullOrEmpty(_version))
                {
                    try
                    {
                        Task<IEnumerable<JObject>> packages = _repoManager.ActiveRepository.GetPackageMetadataById(Id);
                        var r = packages.Result;
                        var allVersions = r.Select(p => NuGetVersion.Parse(p.Value<string>(Properties.Version)));
                        _version = allVersions.OrderByDescending(v => v).FirstOrDefault().ToNormalizedString();
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.HandleException(ex, false);
                    }
                }
                return _version;
            }
            set
            {
                _version = value;
            }
        }

        [Parameter]
        public DependencyBehavior DependencyBehavior { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        public InstallPackageCommand()
        {
            _packageSourceProvider = ServiceLocator.GetInstance<IVsPackageSourceProvider>();
            _repositoryFactory = ServiceLocator.GetInstance<IPackageRepositoryFactory>();
            _serviceProvider = ServiceLocator.GetInstance<SVsServiceProvider>();
            _packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            _repoManager = new VsSourceRepositoryManager(_packageSourceProvider, _repositoryFactory);
            _solutionManager = new SolutionManager();
            _VsContext = new VsPackageManagerContext(_repoManager, _serviceProvider, _solutionManager, _packageManagerFactory);
            _actionResolver = new ActionResolver(_repoManager.ActiveRepository, ResolutionContext);
            _solution = _VsContext.GetCurrentVsSolution();
        }

        public ResolutionContext ResolutionContext
        {
            get
            {
                _context = new ResolutionContext();
                _context.DependencyBehavior = DependencyBehavior;
                _context.AllowPrerelease = IncludePrerelease.IsPresent;
                return _context;
            }
        }

        public PackageIdentity Identity
        {
            get
            {
                _identity = new PackageIdentity(Id, NuGetVersion.Parse(Version));
                return _identity;
            }
        }

        public IEnumerable<VsProject> TargetedProjects
        {
            get
            {         
                EnvDTE.Project project = _solution.DteSolution.GetAllProjects()
                    .FirstOrDefault(p => String.Equals(p.Name, ProjectName, StringComparison.OrdinalIgnoreCase));
                VsProject vsProject = _solution.GetProject(project);
                List<VsProject> targetedProjects = new List<VsProject> {vsProject};
                return targetedProjects;
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

        private void ProcessRecordCore()
        {
            if (!_solutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            try
            {
                SubscribeToProgressEvents();

                // Resolve Actions
                Task<IEnumerable<NuGet.Client.Resolution.PackageAction>> actions = _actionResolver.ResolveActionsAsync(
                    Identity, PackageActionType.Install, TargetedProjects, _solution);

                // Execute Actions
                ActionExecutor executor = new ActionExecutor();                
                Task task = executor.ExecuteActionsAsync(actions.Result, this, CancellationToken.None);
                task.Wait();
            }
            catch (Exception ex)
            {
                this.Log(Client.MessageLevel.Warning, ex.Message);
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

    public  class ProgressRecordCollection : KeyedCollection<int, ProgressRecord>
    {
        protected override int GetKeyForItem(ProgressRecord item)
        {
            return item.ActivityId;
        }
    }
} 
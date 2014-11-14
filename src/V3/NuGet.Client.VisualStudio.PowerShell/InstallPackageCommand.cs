using Microsoft.VisualStudio.Shell;
using NuGet.Client;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;
using NuGet.Client.Resolution;
using NuGet.Client.VisualStudio;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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
    public class InstallPackageCommand : PSCmdlet, IExecutionLogger //, IErrorHandler
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

        public InstallPackageCommand()
        {
            _packageSourceProvider = ServiceLocator.GetInstance<IVsPackageSourceProvider>();
            _repositoryFactory = ServiceLocator.GetInstance<IPackageRepositoryFactory>();
            _serviceProvider = ServiceLocator.GetInstance<SVsServiceProvider>();
            _packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            _repoManager = new VsSourceRepositoryManager(_packageSourceProvider, _repositoryFactory);
            _solutionManager = new SolutionManager();
            _VsContext = new VsPackageManagerContext(_repoManager, _serviceProvider, _solutionManager, _packageManagerFactory);
            _actionResolver = new ActionResolver(_repoManager.ActiveRepository, _context);
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

        public IEnumerable<VsProject> TargetedProject
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
                // ErrorHandler.HandleException(ex, terminating: true);
            }
            finally
            {
                UnsubscribeEvents();
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

        /*
        protected IErrorHandler ErrorHandler
        {
            get
            {
                return this;
            }
        } */

        private void ProcessRecordCore()
        {
            // Resolve Actions
            Task<IEnumerable<NuGet.Client.Resolution.PackageAction>> actions = _actionResolver.ResolveActionsAsync(
                _identity, PackageActionType.Install, TargetedProject, _solution);

            // Execute Actions
            ActionExecutor executor = new ActionExecutor();
            executor.ExecuteActionsAsync(actions.Result, CancellationToken.None);
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public virtual string Id { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public virtual string ProjectName { get; set; }

        [Parameter(Position = 2)]
        [ValidateNotNull]
        public string Version { get; set; }

        [Parameter]
        public DependencyBehavior DependencyBehavior { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        public void Log(Client.MessageLevel level, string message, params object[] args)
        {
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            return FileConflictAction.IgnoreAll;
        }

        /*
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
        } */

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
    }

    public  class ProgressRecordCollection : KeyedCollection<int, ProgressRecord>
    {
        protected override int GetKeyForItem(ProgressRecord item)
        {
            return item.ActivityId;
        }
    }
}
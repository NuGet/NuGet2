using System;
﻿using Microsoft.VisualStudio.Shell;
using NuGet.Client.ProjectSystem;
using NuGet.PowerShell.Commands;
using NuGet.Versioning;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;
using NuGetConsole.Host.PowerShell.Implementation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Runtime.Versioning;

#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
#endif

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This command process the specified package against the specified project.
    /// </summary>
    public abstract class NuGetPowerShellBaseCommand : PSCmdlet, IExecutionContext, IErrorHandler
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
        internal const string PowerConsoleHostName = "Package Manager Host";

        private readonly Lazy<string> _psCommandsUserAgent = new Lazy<string>(
            () => HttpUtility.CreateUserAgentString(PSCommandsUserAgentClient, VsVersionHelper.FullVsEdition));

        private ProgressRecordCollection _progressRecordCache;
        private bool _overwriteAll, _ignoreAll;

        public NuGetPowerShellBaseCommand(
            IVsPackageSourceProvider packageSourceProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            SVsServiceProvider svcServceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ISolutionManager solutionManager,
            IHttpClientEvents clientEvents)
        {
            _packageSourceProvider = packageSourceProvider;
            _repositoryFactory = packageRepositoryFactory;
            _serviceProvider = svcServceProvider;
            _packageManagerFactory = packageManagerFactory;
            _solutionManager = solutionManager;
            _repoManager = new VsSourceRepositoryManager(_packageSourceProvider, _repositoryFactory);
            _VsContext = new VsPackageManagerContext(_repoManager, _serviceProvider, _solutionManager, _packageManagerFactory);
            _httpClientEvents = clientEvents;
        }

        internal VsSolution Solution
        {
            get
            {
                return _VsContext.GetCurrentVsSolution();
            }
        }

        internal IEnumerable<VsProject> Projects { get; set; }

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

        public SourceRepository ActiveSourceRepository { get; set; }

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

        protected void CheckForSolutionOpen()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
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

        public void Log(MessageLevel level, string message, params object[] args)
        {
            string formattedMessage = String.Format(CultureInfo.CurrentCulture, message, args);
            LogCore(level, formattedMessage);
        }

        public virtual FileConflictAction ResolveFileConflict(string message)
        {
            if (_overwriteAll)
            {
                return FileConflictAction.OverwriteAll;
            }

            if (_ignoreAll)
            {
                return FileConflictAction.IgnoreAll;
            }

            var choices = new Collection<ChoiceDescription>
            {
                new ChoiceDescription(Resources.Cmdlet_Yes, Resources.Cmdlet_FileConflictYesHelp),
                new ChoiceDescription(Resources.Cmdlet_YesAll, Resources.Cmdlet_FileConflictYesAllHelp),
                new ChoiceDescription(Resources.Cmdlet_No, Resources.Cmdlet_FileConflictNoHelp),
                new ChoiceDescription(Resources.Cmdlet_NoAll, Resources.Cmdlet_FileConflictNoAllHelp)
            };

            int choice = Host.UI.PromptForChoice(VsResources.FileConflictTitle, message, choices, defaultChoice: 2);

            Debug.Assert(choice >= 0 && choice < 4);
            switch (choice)
            {
                case 0:
                    return FileConflictAction.Overwrite;

                case 1:
                    _overwriteAll = true;
                    return FileConflictAction.OverwriteAll;

                case 2:
                    return FileConflictAction.Ignore;

                case 3:
                    _ignoreAll = true;
                    return FileConflictAction.IgnoreAll;
            }

            return FileConflictAction.Ignore;
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

        protected virtual void LogCore(MessageLevel level, string formattedMessage)
        {
            switch (level)
            {
                case MessageLevel.Debug:
                    WriteVerbose(formattedMessage);
                    break;

                case MessageLevel.Warning:
                    WriteWarning(formattedMessage);
                    break;

                case MessageLevel.Info:
                    WriteLine(formattedMessage);
                    break;

                case MessageLevel.Error:
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

        #endregion Logging

        #region Project APIs

        /// <summary>
        /// Get the VsProject by ProjectName. 
        /// If ProjectName is not specified, return the Default project of Tool window.
        /// </summary>
        /// <param name="throwIfNotExists"></param>
        /// <returns></returns>
        public VsProject GetProject(string projectName, bool throwIfNotExists)
        {
            VsProject project = null;

            // If the user does not specify a project then use the Default project
            if (String.IsNullOrEmpty(projectName))
            {
                projectName = SolutionManager.DefaultProjectName;
            }

            EnvDTE.Project dteProject = Solution.DteSolution.GetAllProjects()
                .FirstOrDefault(p => String.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase) ||
                                     String.Equals(p.FullName, projectName, StringComparison.OrdinalIgnoreCase));
            if (dteProject != null)
            {
                project = Solution.GetProject(dteProject);
            }

            // If that project was invalid then throw
            if (project == null && throwIfNotExists)
            {
                ErrorHandler.ThrowNoCompatibleProjectsTerminatingError();
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
        protected IEnumerable<EnvDTE.Project> GetProjectsByName(string[] projectNames)
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
                    yield return project;
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
        /// Return all projects in current solution.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<VsProject> GetAllProjectsInSolution()
        {
            IEnumerable<string> projectNames = GetAllValidProjectNames();
            return GetProjectsByName(projectNames);
        }

        /// <summary>
        /// Return all projects in the solution matching the provided names. Wildcards are supported.
        /// This method will automatically generate error records for non-wildcarded project names that
        /// are not found.
        /// </summary>
        /// <param name="projectNames">An array of project names that may or may not include wildcards.</param>
        /// <returns>Projects matching the project name(s) provided.</returns>
        protected IEnumerable<VsProject> GetProjectsByName(IEnumerable<string> projectNames)
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
        protected IEnumerable<string> GetAllValidProjectNames()
        {
            var safeNames = _solutionManager.GetProjects().Select(p => _solutionManager.GetProjectSafeName(p));
            var uniqueNames = _solutionManager.GetProjects().Select(p => p.GetCustomUniqueName());
            return uniqueNames.Concat(safeNames).Distinct();
        }

        /// <summary>
        /// This method will set the default project of PowerShell Console by project name.
        /// </summary>
        /// <param name="projectNames">The project name to be set to.</param>
        /// <returns>Boolean indicating success or failure.</returns>
        protected bool SetProjectsByName(string projectName)
        {
            var host = PowerShellHostService.CreateHost(PowerConsoleHostName, false);
            var allValidProjectNames = host.GetAvailableProjects().ToList();
            string match = allValidProjectNames.Where(p => string.Equals(p, projectName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            int matchIndex;
            if (string.IsNullOrEmpty(match))
            {
                ErrorHandler.WriteProjectNotFoundError(projectName, terminating: false);
                return false;
            }
            else
            {
                try
                {
                    matchIndex = allValidProjectNames.IndexOf(match);
                    host.SetDefaultProjectIndex(matchIndex);
                    _solutionManager.DefaultProjectName = match;
                    Log(MessageLevel.Info, Resources.Cmdlet_ProjectSet, match);
                    return true;
                }
                catch (Exception ex)
                {
                    WriteError(ex);
                    return false;
                }
            }
        }
        #endregion Project APIs

        /// <summary>
        /// This method will set the active package source of PowerShell Console by source name.
        /// </summary>
        /// <param name="projectNames">The project name to be set to.</param>
        /// <returns>Boolean indicating success or failure.</returns>
        protected bool SetPackageSourceByName(string sourceName)
        {
            var host = PowerShellHostService.CreateHost(PowerConsoleHostName, false);
            var allSourceNames = host.GetPackageSources().ToList();
            string match = allSourceNames.Where(p => string.Equals(p, sourceName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (string.IsNullOrEmpty(match))
            {
                Log(MessageLevel.Error, Resources.Cmdlet_PackageSourceNotFound, sourceName);
                return false;
            }
            else
            {
                try
                {
                    host.ActivePackageSource = match;
                    this.ActiveSourceRepository = GetActiveRepository(match);
                    Log(MessageLevel.Info, Resources.Cmdlet_PackageSourceSet, match);
                    return true;
                }
                catch (Exception ex)
                {
                    WriteError(ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the active SourceRepository for current solution.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected SourceRepository GetActiveRepository(string source)
        {
            SourceRepository activeSourceRepository = null;
            if (!string.IsNullOrEmpty(source))
            {
                activeSourceRepository = CreateRepositoryFromSource(source);
            }
            else if (activeSourceRepository == null)
            {
                activeSourceRepository = _repoManager.ActiveRepository;
            }
            return activeSourceRepository;
        }

        /// <summary>
        /// Create a package repository from the source by trying to resolve relative paths.
        /// </summary>
        protected SourceRepository CreateRepositoryFromSource(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            UriFormatException uriException = null;
            string url = _packageSourceProvider.ResolveSource(source);

            try
            {
                PackageSource packageSource = new PackageSource(source, url);
                var sourceRepo = new AutoDetectSourceRepository(packageSource, PSCommandsUserAgentClient, _repositoryFactory);
                return sourceRepo;
            }
            catch (UriFormatException ex)
            {
                // if the source is relative path, it can result in invalid uri exception
                uriException = ex;
            }

            return null;
        }

        public void ExecuteScript(string packageInstallPath, string scriptRelativePath, object packageObject, Installation.InstallationTarget target)
        {
            IPackage package = (IPackage)packageObject;

            // If we don't have a project, we're at solution level
            string projectName = target.Name;
            FrameworkName targetFramework = target.GetSupportedFrameworks().FirstOrDefault();

            VsProject targetProject = target as VsProject;
            EnvDTE.Project project = targetProject == null ? null : targetProject.DteProject;
            string fullPath = Path.Combine(packageInstallPath, scriptRelativePath);

            if (!File.Exists(fullPath))
            {
                VsNuGetTraceSources.VsPowerShellScriptExecutionFeature.Error(
                    "missing_script",
                    "[{0}] Unable to locate expected script file: {1}",
                    projectName,
                    fullPath);
            }
            else
            {
                var psVariable = SessionState.PSVariable;
                string toolsPath = Path.GetDirectoryName(fullPath);

                // set temp variables to pass to the script
                psVariable.Set("__rootPath", packageInstallPath);
                psVariable.Set("__toolsPath", toolsPath);
                psVariable.Set("__package", package);
                psVariable.Set("__project", project);

                string command = "& " + PathHelper.EscapePSPath(fullPath) + " $__rootPath $__toolsPath $__package $__project";
                Log(MessageLevel.Info, String.Format(CultureInfo.CurrentCulture, VsResources.ExecutingScript, fullPath));

                InvokeCommand.InvokeScript(command, false, PipelineResultTypes.Error, null, null);

                // clear temp variables
                psVariable.Remove("__rootPath");
                psVariable.Remove("__toolsPath");
                psVariable.Remove("__package");
                psVariable.Remove("__project");
            }
        }

        public void OpenFile(string fullPath)
        {
            var commonOperations = ServiceLocator.GetInstance<IVsCommonOperations>();
            commonOperations.OpenFile(fullPath);
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
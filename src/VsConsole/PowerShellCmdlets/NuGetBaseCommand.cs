using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This is the base class for all NuGet cmdlets.
    /// </summary>
    public abstract class NuGetBaseCommand : PSCmdlet
    {
        // User Agent. Do NOT localize
        private const string PSCommandsUserAgentClient = "NuGet VS PowerShell Console";
        private readonly Lazy<string> _psCommandsUserAgent = new Lazy<string>(
            () => HttpUtility.CreateUserAgentString(PSCommandsUserAgentClient, VsVersionHelper.FullVsEdition));

        private IVsPackageManager _packageManager;
        private readonly ISolutionManager _solutionManager;
        private readonly IVsPackageManagerFactory _vsPackageManagerFactory;
        private ProgressRecordCollection _progressRecordCache;
        private readonly IHttpClientEvents _httpClientEvents;
        private readonly IErrorHandler _errorHandler;
        private ILogger _logger;

        protected NuGetBaseCommand(
            ISolutionManager solutionManager, 
            IVsPackageManagerFactory vsPackageManagerFactory, 
            IHttpClientEvents httpClientEvents)
        {
            _solutionManager = solutionManager;
            _vsPackageManagerFactory = vsPackageManagerFactory;
            _httpClientEvents = httpClientEvents;
            _errorHandler = new PowerShellCmdletErrorHandler(this);
        }
        
        protected virtual ILogger Logger
        {
            get
            {
                return _logger ?? (_logger = new PowerShellCmdletLogger(this, ErrorHandler));
            }
        }

        protected IErrorHandler ErrorHandler { get { return _errorHandler; } }

        protected string DefaultUserAgent
        {
            get
            {
                return _psCommandsUserAgent.Value;
            }
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

        protected ISolutionManager SolutionManager
        {
            get
            {
                return _solutionManager;
            }
        }

        protected IVsPackageManagerFactory PackageManagerFactory
        {
            get
            {
                return _vsPackageManagerFactory;
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

        /// <summary>
        /// Gets an instance of VSPackageManager to be used throughout the execution of this command.
        /// </summary>
        /// <value>The package manager.</value>
        protected internal IVsPackageManager PackageManager
        {
            get
            {
                if (_packageManager == null)
                {
                    _packageManager = CreatePackageManager();
                }

                return _packageManager;
            }
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
                _errorHandler.HandleException(ex, terminating: true);
            }
            finally
            {
                UnsubscribeEvents();
            }
        }

        protected override void StopProcessing()
        {
            UnsubscribeEvents();
            base.StopProcessing();
        }

        /// <summary>
        /// Derived classess must implement this method instead of ProcessRecord(), which is sealed by NuGetBaseCmdlet.
        /// </summary>
        protected abstract void ProcessRecordCore();
        
        internal void Execute()
        {
            BeginProcessing();
            ProcessRecord();
            EndProcessing();
        }

        protected override void BeginProcessing()
        {
            if (_httpClientEvents != null)
            {
                _httpClientEvents.SendingRequest += OnSendingRequest;
            }
        }

        protected override void EndProcessing()
        {
            UnsubscribeEvents();
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

        protected virtual IVsPackageManager CreatePackageManager()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                return null;
            }

            return PackageManagerFactory.CreatePackageManager();
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
                    yield return project;
                }

                // We only emit non-terminating error record if a non-wildcarded name was not found.
                // This is consistent with built-in cmdlets that support wildcarded search.
                // A search with a wildcard that returns nothing should not be considered an error.
                if ((count == 0) && !WildcardPattern.ContainsWildcardCharacters(projectName))
                {
                    _errorHandler.WriteProjectNotFoundError(projectName, terminating: false);
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

        /// <summary>
        /// Translate a PSPath into a System.IO.* friendly Win32 path.
        /// Does not resolve/glob wildcards.
        /// </summary>                
        /// <param name="psPath">The PowerShell PSPath to translate which may reference PSDrives or have provider-qualified paths which are syntactically invalid for .NET APIs.</param>
        /// <param name="path">The translated PSPath in a format understandable to .NET APIs.</param>
        /// <param name="exists">Returns null if not tested, or a bool representing path existence.</param>
        /// <param name="errorMessage">If translation failed, contains the reason.</param>
        /// <returns>True if successfully translated, false if not.</returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#", Justification = "Following TryParse pattern in BCL", Target = "path")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Justification = "Following TryParse pattern in BCL", Target = "exists")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "ps", Justification = "ps is a common powershell prefix")]
        protected bool TryTranslatePSPath(string psPath, out string path, out bool? exists, out string errorMessage)
        {
            return PSPathUtility.TryTranslatePSPath(SessionState, psPath, out path, out exists, out errorMessage);
        }

        /// <summary>
        /// Create a package repository from the source by trying to resolve relative paths.
        /// </summary>
        protected IPackageRepository CreateRepositoryFromSource(IPackageRepositoryFactory repositoryFactory, IPackageSourceProvider sourceProvider, string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            UriFormatException uriException = null;

            string resolvedSource = sourceProvider.ResolveSource(source);
            try
            {
                IPackageRepository repository = repositoryFactory.CreateRepository(resolvedSource);
                if (repository != null)
                {
                    return repository;
                }
            }
            catch (UriFormatException ex)
            {
                // if the source is relative path, it can result in invalid uri exception
                uriException = ex;
            }

            Uri uri;
            // if it's not an absolute path, treat it as relative path
            if (Uri.TryCreate(source, UriKind.Relative, out uri))
            {
                string outputPath;
                bool? exists;
                string errorMessage;
                // translate relative path to absolute path
                if (TryTranslatePSPath(source, out outputPath, out exists, out errorMessage) && exists == true)
                {
                    return repositoryFactory.CreateRepository(outputPath);
                }
                else
                {
                    return repositoryFactory.CreateRepository(source);
                }
            }
            else
            {
                // if this is not a valid relative path either, 
                // we rethrow the UriFormatException that we caught earlier.
                if (uriException != null)
                {
                    throw uriException;
                }
            }
            return null;
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

        protected virtual void OnSendingRequest(object sender, WebRequestEventArgs e)
        {
            HttpUtility.SetUserAgent(e.Request, _psCommandsUserAgent.Value);
        }
        
        private void UnsubscribeEvents()
        {
            if (_httpClientEvents != null)
            {
                _httpClientEvents.SendingRequest -= OnSendingRequest;
            }
        }

        private class ProgressRecordCollection : KeyedCollection<int, ProgressRecord>
        {
            protected override int GetKeyForItem(ProgressRecord item)
            {
                return item.ActivityId;
            }
        }
    }
}
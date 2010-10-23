using System;
using System.Globalization;
using System.Management.Automation;
using EnvDTE;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This is the base class for all NuPack cmdlets.
    /// </summary>
    public abstract class NuPackBaseCmdlet : PSCmdlet, ILogger {
        private IVsPackageManager _packageManager;
        private readonly ISolutionManager _solutionManager;
        private readonly DTE _dte;

        protected NuPackBaseCmdlet()
            : this(NuPack.VisualStudio.SolutionManager.Current, dte: DTEExtensions.DTE, packageManager: null) { }

        protected NuPackBaseCmdlet(ISolutionManager solutionManager, DTE dte, VSPackageManager packageManager) {
            _solutionManager = solutionManager;
            _dte = dte;
            _packageManager = packageManager;
        }

        protected ISolutionManager SolutionManager {
            get {
                return _solutionManager;
            }
        }

        protected DTE DTE {
            get {
                return _dte;
            }
        }

        /// <summary>
        /// Gets an instance of VSPackageManager to be used throughout the execution of this command.
        /// </summary>
        /// <value>The package manager.</value>
        protected virtual IVsPackageManager PackageManager {
            get {
                if (_packageManager == null) {
                    _packageManager = GetPackageManager();
                }

                return _packageManager;
            }
            set {
                _packageManager = value;
            }
        }

        /// <summary>
        /// Gets the default project name if a -Project parameter is not supplied.
        /// </summary>
        /// <value>The default project name.</value>
        protected string DefaultProjectName {
            get {
                return SolutionManager.DefaultProjectName;
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is a solution open in the IDE.
        /// </summary>
        protected bool IsSolutionOpen {
            get {
                return DTE != null && DTE.Solution != null && DTE.Solution.IsOpen;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We want to display friendly message to the console.")]
        protected sealed override void ProcessRecord() {
            try {
                ProcessRecordCore();
            }
            catch (Exception ex) {
                WriteError(ex.InnerException ?? ex);
            }
        }

        /// <summary>
        /// Derived classess must implement this method instead of ProcessRecord(), which is sealed by NuPackBaseCmdlet.
        /// </summary>
        protected abstract void ProcessRecordCore();

        void ILogger.Log(MessageLevel level, string message, params object[] args) {
            string formattedMessage = String.Format(CultureInfo.CurrentCulture, message, args);
            Log(level, formattedMessage);
        }

        public void Execute() {
            BeginProcessing();
            ProcessRecord();
            EndProcessing();
        }

        protected void Log(MessageLevel level, string formattedMessage) {
            switch (level) {
                case MessageLevel.Debug:
                    WriteVerbose(formattedMessage);
                    break;

                case MessageLevel.Warning:
                    WriteWarning(formattedMessage);
                    break;

                case MessageLevel.Info:
                    WriteLine(formattedMessage);
                    break;
            }
        }

        private static IVsPackageManager GetPackageManager() {
            if (!IsSolutionOpen) {
                return null;
            }

            // prepare a PackageManager instance for use throughout the command execution lifetime
            DTE dte = DTEExtensions.DTE;
            if (dte == null) {
                throw new InvalidOperationException("DTE isn't loaded.");
            }
            return new VsPackageManager(dte);
        }

        protected static IVsPackageManager GetPackageManager(string source) {
            if (!IsSolutionOpen) {
                return null;
            }

            // prepare a PackageManager instance for use throughout the command execution lifetime
            if (DTE == null) {
                throw new InvalidOperationException("DTE isn't loaded.");
            }

            return new VsPackageManager(dte, PackageRepositoryFactory.Default.CreateRepository(source));
        }

        protected Project GetProjectFromName(string projectName) {
            return SolutionManager.GetProject(projectName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Usage",
            "CA2201:DoNotRaiseReservedExceptionTypes",
            Justification = "This exception is passed to PowerShell. We really don't care about the type of exception here.")]
        protected void WriteError(string message) {
            if (!String.IsNullOrEmpty(message)) {
                WriteError(new Exception(message));
            }
        }

        protected void WriteError(Exception exception) {
            WriteError(new ErrorRecord(exception, String.Empty, ErrorCategory.NotSpecified, null));
        }

        protected void WriteLine(string message = null) {
            if (message == null) {
                Host.UI.WriteLine();
            }
            else {
                Host.UI.WriteLine(message);
            }
        }
    }
}
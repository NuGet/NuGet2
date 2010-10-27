using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using EnvDTE;

namespace NuGet.VisualStudio.Cmdlets {

    /// <summary>
    /// This is the base class for all NuGet cmdlets.
    /// </summary>
    public abstract class NuGetBaseCmdlet : PSCmdlet, ILogger {
        private IVsPackageManager _packageManager;
        private readonly ISolutionManager _solutionManager;
        private readonly IVsPackageManagerFactory _vsPackageManagerFactory;

        protected NuGetBaseCmdlet(ISolutionManager solutionManager, IVsPackageManagerFactory vsPackageManagerFactory) {
            _solutionManager = solutionManager;
            _vsPackageManagerFactory = vsPackageManagerFactory;
        }

        protected ISolutionManager SolutionManager {
            get {
                return _solutionManager;
            }
        }

        protected IVsPackageManagerFactory PackageManagerFactory {
            get {
                return _vsPackageManagerFactory;
            }
        }

        /// <summary>
        /// Gets an instance of VSPackageManager to be used throughout the execution of this command.
        /// </summary>
        /// <value>The package manager.</value>
        protected internal IVsPackageManager PackageManager {
            get {
                if (_packageManager == null) {
                    _packageManager = CreatePackageManager();
                }

                return _packageManager;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to display friendly message to the console.")]
        protected sealed override void ProcessRecord() {
            try {
                ProcessRecordCore();
            }
            catch (Exception ex) {
                WriteError(ex.InnerException ?? ex);
            }
        }

        /// <summary>
        /// Derived classess must implement this method instead of ProcessRecord(), which is sealed by NuGetBaseCmdlet.
        /// </summary>
        protected abstract void ProcessRecordCore();

        void ILogger.Log(MessageLevel level, string message, params object[] args) {
            string formattedMessage = String.Format(CultureInfo.CurrentCulture, message, args);
            Log(level, formattedMessage);
        }

        internal void Execute() {
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

        protected virtual IVsPackageManager CreatePackageManager() {
            if (!SolutionManager.IsSolutionOpen) {
                return null;
            }

            return PackageManagerFactory.CreatePackageManager();
        }


        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "This exception is passed to PowerShell. We really don't care about the type of exception here.")]
        protected void WriteError(string message) {
            if (!String.IsNullOrEmpty(message)) {
                WriteError(new Exception(message));
            }
        }

        protected void WriteError(Exception exception) {
            WriteError(new ErrorRecord(exception, String.Empty, ErrorCategory.NotSpecified, null));
        }

        protected void WriteLine(string message = null) {
            if (Host == null) {
                // Host is null when running unit tests. Simply return in this case
                return;
            }

            if (message == null) {
                Host.UI.WriteLine();
            }
            else {
                Host.UI.WriteLine(message);
            }
        }
    }
}

using System;
using System.Globalization;
using System.Management.Automation;
using EnvDTE;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This is the base class for all NuPack cmdlets.
    /// </summary>
    public abstract class NuPackBaseCmdlet : PSCmdlet, ILogger {

        private VSPackageManager _packageManager;

        /// <summary>
        /// Gets an instance of VSPackageManager to be used throughout the execution of this command.
        /// </summary>
        /// <value>The package manager.</value>
        public VSPackageManager PackageManager {
            get {
                if (_packageManager == null) {
                    _packageManager = GetPackageManager();
                }

                return _packageManager;
            }
        }

        /// <summary>
        /// Gets the default project name if a -Project parameter is not supplied.
        /// </summary>
        /// <value>The default project name.</value>
        protected string DefaultProjectName {
            get {
                return SolutionProjectsHelper.Instance.DefaultProjectName;
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is a solution open in the IDE.
        /// </summary>
        protected bool IsSolutionOpen {
            get {
                var dte = DTEExtensions.DTE;
                return dte != null && dte.Solution != null && dte.Solution.IsOpen;
            }
        }

        #region Processing methods

        protected sealed override void ProcessRecord() {
            try {
                ProcessRecordCore();
            }
            catch (Exception ex) {
                // Swallowing exception here has two purposes: 
                // + display friendly error message to the console without the stack trace.
                // + allows EndProcessing() to get called, so that we can unsubscribe the Logger.

                if (ex.InnerException != null) {
                    WriteError(ex.InnerException.Message);
                }
                else {
                    WriteError(ex.Message);
                }
            }
        }

        protected override void EndProcessing() {
            if (_packageManager != null) {
                _packageManager.Logger = null;
            }
        }

        /// <summary>
        /// Derived classess must implement this method instead of ProcessRecord(), which is sealed by NuPackBaseCmdlet.
        /// </summary>
        protected abstract void ProcessRecordCore();

        #endregion

        #region ILogger implementation

        void ILogger.Log(MessageLevel level, string message, params object[] args) {
            string formattedMessage = String.Format(CultureInfo.CurrentCulture, message, args);
            Log(level, formattedMessage);
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

        #endregion

        #region Helper functions

        private VSPackageManager GetPackageManager() {
            if (!IsSolutionOpen) {
                return null;
            }

            // prepare a PackageManager instance for use throughout the command execution lifetime
            DTE dte = DTEExtensions.DTE;
            if (dte == null) {
                throw new InvalidOperationException("DTE isn't loaded.");
            }

            var packageManager = VSPackageManager.GetPackageManager(dte);
            packageManager.Logger = this;

            return packageManager;
        }

        protected Project GetProjectFromName(string projectName) {
            return SolutionProjectsHelper.Instance.GetProjectFromName(projectName);
        }

        protected void WriteError(string message) {
            if (!String.IsNullOrEmpty(message)) {
                Host.UI.WriteErrorLine(message);
            }
        }

        protected void WriteLine(string message = null) {
            if (message == null) {
                Host.UI.WriteLine();
            }
            else {
                Host.UI.WriteLine(message);
            }
        }

        #endregion
    }
}
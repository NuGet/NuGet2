using System;
using System.Globalization;
using System.Management.Automation;
using EnvDTE;

namespace NuPack.VisualStudio.Cmdlets {

    public abstract class NuPackBaseCmdlet : PSCmdlet, ILogger {

        private VSPackageManager _packageManager;
        public VSPackageManager PackageManager {
            get {
                if (_packageManager == null) {
                    _packageManager = GetPackageManager();
                }

                return _packageManager;
            }
        }

        protected string DefaultProjectName {
            get {
                return SolutionProjectsHelper.Instance.DefaultProjectName;
            }
        }

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
                // IMPORTANT: we need to swallow exception here so that the EndProcessing() method is called.
                WriteError(new ErrorRecord(ex, null, ErrorCategory.NotSpecified, null));
            }
        }

        protected override void EndProcessing() {
            if (_packageManager != null) {
                _packageManager.Logger = null;
            }
        }

        /// <summary>
        /// Derived classess must implement this method instead of ProcessRecord(), which is now sealed.
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

        protected void WriteError(string message, string errorId) {
            WriteError(new ErrorRecord(new Exception(message), errorId, ErrorCategory.NotSpecified, null));
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
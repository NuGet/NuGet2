using System;
using System.Globalization;
using System.Management.Automation;
using EnvDTE;
using NuPack.VisualStudio.Resources;
using System.IO;

namespace NuPack.VisualStudio.Cmdlets {

    public abstract class NuPackBaseCmdlet : PSCmdlet, ILogger {

        protected VSPackageManager PackageManager {
            get;
            private set;
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

        protected override void BeginProcessing() {
            // prepare a PackageManager instance for use throughout the command execution lifetime

            DTE dte = DTEExtensions.DTE;
            if (dte == null) {
                throw new InvalidOperationException("DTE isn't loaded.");
            }

            var packageManager = VSPackageManager.GetPackageManager(dte);
            packageManager.Logger = this;

            PackageManager = packageManager;
        }

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
            PackageManager.Logger = null;
        }

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
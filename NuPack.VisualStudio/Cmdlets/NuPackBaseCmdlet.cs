using System;
using System.Globalization;
using System.Management.Automation;
using EnvDTE;
using NuPack.VisualStudio.Resources;

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

        #region Processing methods

        protected override void BeginProcessing() {
            // prepare a PackageManager instance for use throughout the command execution lifetime

            DTE dte = DTEExtensions.DTE;
            if (dte == null) {
                throw new InvalidOperationException("DTE isn't loaded.");
            }

            var packageManager = VSPackageManager.GetPackageManager(dte);
            packageManager.Logger = this;
            packageManager.PackageInstalling += PackageManagerPackageInstalling;
            packageManager.PackageInstalled += PackageManagerPackageInstalled;
            packageManager.PackageUninstalling += PackageManagerPackageUninstalling;
            packageManager.PackageUninstalled += PackageManagerPackageUninstalled;

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

        protected abstract void ProcessRecordCore();

        protected override void EndProcessing() {
            var packageManager = PackageManager;
            if (packageManager != null) {
                packageManager.Logger = null;
                packageManager.PackageInstalling -= PackageManagerPackageInstalling;
                packageManager.PackageInstalled -= PackageManagerPackageInstalled;
                packageManager.PackageUninstalling -= PackageManagerPackageUninstalling;
                packageManager.PackageUninstalled -= PackageManagerPackageUninstalled;
            }
        }

        private void PackageManagerPackageUninstalled(object sender, PackageOperationEventArgs e) {
            OnPackageUninstalled(e);
        }

        private void PackageManagerPackageUninstalling(object sender, PackageOperationEventArgs e) {
            OnPackageUninstalling(e);
        }

        private void PackageManagerPackageInstalled(object sender, PackageOperationEventArgs e) {
            OnPackageInstalled(e);
        }

        private void PackageManagerPackageInstalling(object sender, PackageOperationEventArgs e) {
            OnPackageInstalling(e);
        }

        protected virtual void OnPackageInstalling(PackageOperationEventArgs e) {
            WriteDisclaimerText(e.Package);
        }

        protected virtual void OnPackageInstalled(PackageOperationEventArgs e) {
        }

        protected virtual void OnPackageUninstalling(PackageOperationEventArgs e) {
        }

        protected virtual void OnPackageUninstalled(PackageOperationEventArgs e) {
        }

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

        protected ProjectManager GetProjectManager(string projectName) {
            if (projectName == null) {
                throw new ArgumentNullException("projectName");
            }

            Project project = GetProjectFromName(projectName);
            ProjectManager projectManager = PackageManager.GetProjectManager(project);
            return projectManager;
        }

        protected Project GetProjectFromName(string projectName) {
            return SolutionProjectsHelper.Instance.GetProjectFromName(projectName);
        }

        protected void WriteError(string message, string errorId) {
            WriteError(new ErrorRecord(new Exception(message), errorId, ErrorCategory.NotSpecified, null));
        }

        protected void WriteDisclaimerText(IPackage package) {
            if (package.RequireLicenseAcceptance) {
                string message = String.Format(
                    CultureInfo.CurrentCulture,
                    VsResources.InstallSuccessDisclaimerText,
                    package.Id,
                    package.GetAuthorsDisplayString(),
                    package.LicenseUrl);

                WriteLine(message);
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
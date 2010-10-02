using System;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;

namespace NuPack.VisualStudio.Cmdlets {

    [Cmdlet(VerbsCommon.Remove, "Package")]
    public class RemovePackageCmdlet : NuPackBaseCmdlet {

        #region Parameters

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public string Id { get; set; }

        [Parameter(Position = 1)]
        public string Project { get; set; }

        [Parameter(Position = 2)]
        public SwitchParameter Force { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter RemoveDependencies { get; set; }

        #endregion

        protected override void ProcessRecordCore() {
            try {
                var packageManager = PackageManager;
                Debug.Assert(packageManager != null);

                bool isSolutionLevel = CmdletHelper.IsSolutionOnlyPackage(packageManager.LocalRepository, Id);
                if (isSolutionLevel) {
                    if (!String.IsNullOrEmpty(Project)) {
                        WriteError(
                            String.Format(CultureInfo.CurrentCulture, "The package '{0}' only applies to the solution and not to a project. Remove the -Project parameter.", Id),
                            "Remove-Package");
                    }
                    else {
                        packageManager.UninstallPackage(Id, null, Force.IsPresent, RemoveDependencies.IsPresent);
                    }
                }
                else {
                    string projectName = Project;
                    if (String.IsNullOrEmpty(projectName)) {
                        projectName = DefaultProjectName;
                    }

                    if (!String.IsNullOrEmpty(projectName)) {
                        var projectManager = GetProjectManager(projectName);
                        projectManager.Logger = this;
                        projectManager.RemovePackageReference(Id, Force.IsPresent, RemoveDependencies.IsPresent);
                        projectManager.Logger = null;
                    }
                    else {
                        WriteError("Missing project parameter and the default project is not set.", "Remove-Package");
                    }
                }
            }
            catch (Exception ex) {
                WriteError(new ErrorRecord(ex, "Remove-Package", ErrorCategory.NotSpecified, null));
            }
        }

        protected override void EndProcessing() {
            base.EndProcessing();
            WriteLine();
        }
    }
}

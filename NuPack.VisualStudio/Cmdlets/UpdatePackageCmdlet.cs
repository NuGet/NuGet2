using System;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;

namespace NuPack.VisualStudio.Cmdlets {

    [Cmdlet(VerbsData.Update, "Package")]
    public class UpdatePackageCmdlet : NuPackBaseCmdlet {

        #region Parameters

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public string Id { get; set; }

        [Parameter(Position = 1)]
        public string Project { get; set; }

        [Parameter(Position = 2)]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter UpdateDependencies { get; set; }

        #endregion

        protected override void ProcessRecordCore() {
            var packageManager = PackageManager;
            Debug.Assert(packageManager != null);

            bool isSolutionLevelPackage = CmdletHelper.IsSolutionOnlyPackage(packageManager.LocalRepository, Id, Version);

            if (isSolutionLevelPackage) {
                if (!String.IsNullOrEmpty(Project)) {
                    WriteError(
                        String.Format(CultureInfo.CurrentCulture, "The package '{0}' only applies to the solution and not to a project. Remove the -Project parameter.", Id),
                        "Update-Package");
                }
                else {
                    packageManager.UpdatePackage(Id, Version, UpdateDependencies.IsPresent);
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
                    projectManager.UpdatePackageReference(Id, Version, UpdateDependencies.IsPresent);
                    projectManager.Logger = null;
                }
                else {
                    packageManager.UpdatePackage(Id, Version, UpdateDependencies);
                }
            }
        }

        protected override void EndProcessing() {
            base.EndProcessing();
            WriteLine();
        }
    }
}
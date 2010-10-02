using System;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;

namespace NuPack.VisualStudio.Cmdlets
{
    [Cmdlet(VerbsCommon.Add, "Package")]
    public class AddPackageCmdlet : NuPackBaseCmdlet
    {
        #region Parameters

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position=0)]
        public string Id { get; set; }

        [Parameter(Position=1)]
        public string Project { get; set; }

        [Parameter(Position=2)]
        public Version Version { get; set; }

        [Parameter(Position=3)]
        public SwitchParameter IgnoreDependencies { get; set; }

        #endregion

        protected override void ProcessRecordCore()
        {
            var packageManager = PackageManager;
            Debug.Assert(packageManager != null);

            bool isSolutionLevelPackage = CmdletHelper.IsSolutionOnlyPackage(packageManager.SourceRepository, Id, Version);
            
            if (isSolutionLevelPackage) {
                if (!String.IsNullOrEmpty(Project)) {
                    WriteError(
                        String.Format(CultureInfo.CurrentCulture, "The package '{0}' only applies to the solution and not to a project. Remove the -Project parameter.", Id),
                        "Add-Package");
                }
                else {
                    packageManager.InstallPackage(Id, Version, IgnoreDependencies.IsPresent);
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
                    projectManager.AddPackageReference(Id, Version, IgnoreDependencies.IsPresent);
                    projectManager.Logger = null;
                }
                else {
                    WriteError("Missing project parameter and the default project is not set.", "Add-Package");
                }
            }
        }

        protected override void EndProcessing() {
            base.EndProcessing();
            WriteLine();
        }
    }
}
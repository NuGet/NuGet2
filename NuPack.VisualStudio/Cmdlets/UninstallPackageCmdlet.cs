using System;
using System.Globalization;
using System.Management.Automation;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This command uninstalls the specified package from the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Uninstall, "Package")]
    public class UninstallPackageCmdlet : ProcessPackageBaseCmdlet {
        #region Parameters

        [Parameter(Position = 2)]
        public SwitchParameter Force { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter RemoveDependencies { get; set; }

        #endregion

        protected override void ProcessRecordCore() {
            if (!IsSolutionOpen) {
                WriteError("There is no active solution in the current environment.");
                return;
            }

            var packageManager = PackageManager;

            bool isSolutionLevel = IsSolutionOnlyPackage(packageManager.LocalRepository, Id);
            if (isSolutionLevel) {
                if (!String.IsNullOrEmpty(Project)) {
                    WriteError(String.Format(
                        CultureInfo.CurrentCulture,
                        "The package '{0}' only applies to the solution and not to a project. Remove the -Project parameter.",
                        Id));
                }
                else {
                    packageManager.UninstallPackage(Id, null, Force.IsPresent, RemoveDependencies.IsPresent);
                }
            }
            else {
                var projectManager = ProjectManager;
                if (projectManager != null) {
                    projectManager.RemovePackageReference(Id, Force.IsPresent, RemoveDependencies.IsPresent);
                }
                else {
                    WriteError("Missing project parameter or invalid project name.");
                }
            }
        }
    }
}

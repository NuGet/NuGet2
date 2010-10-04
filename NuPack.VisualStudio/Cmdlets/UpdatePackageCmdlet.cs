using System;
using System.Globalization;
using System.Management.Automation;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This project updates the specfied package to the specfied project.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Package")]
    public class UpdatePackageCmdlet : ProcessPackageBaseCmdlet {

        #region Parameters

        [Parameter(Position = 2)]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter UpdateDependencies { get; set; }

        #endregion

        protected override void ProcessRecordCore() {
            if (!IsSolutionOpen) {
                WriteError("There is no active solution.");
                return;
            }

            var packageManager = PackageManager;
            bool isSolutionLevelPackage = IsSolutionOnlyPackage(packageManager.LocalRepository, Id, Version);

            if (isSolutionLevelPackage) {
                if (!String.IsNullOrEmpty(Project)) {
                    WriteError(String.Format(
                        CultureInfo.CurrentCulture,
                        "The package '{0}' only applies to the solution and not to a project. Remove the -Project parameter.",
                        Id));
                }
                else {
                    packageManager.UpdatePackage(Id, Version, UpdateDependencies.IsPresent);
                }
            }
            else {
                var projectManager = ProjectManager;
                if (projectManager != null) {
                    projectManager.UpdatePackageReference(Id, Version, UpdateDependencies.IsPresent);
                }
                else {
                    // if there is no project specified, update at the solution level
                    packageManager.UpdatePackage(Id, Version, UpdateDependencies);
                }
            }
        }
    }
}
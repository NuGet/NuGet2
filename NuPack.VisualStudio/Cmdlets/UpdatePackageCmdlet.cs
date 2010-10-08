using System;
using System.Globalization;
using System.Management.Automation;
using NuPack.VisualStudio.Resources;

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
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            var packageManager = PackageManager;
            bool isSolutionLevelPackage = IsSolutionOnlyPackage(packageManager.LocalRepository, Id, Version);

            if (isSolutionLevelPackage) {
                if (!String.IsNullOrEmpty(Project)) {
                    WriteError(String.Format(
                        CultureInfo.CurrentCulture,
                        VsResources.Cmdlet_PackageForSolutionOnly,
                        Id));
                }
                else {
                    using (new LoggerDisposer(packageManager, this)) {
                        packageManager.UpdatePackage(Id, Version, UpdateDependencies.IsPresent);
                    }
                }
            }
            else {
                var projectManager = ProjectManager;
                if (projectManager != null) {
                    using (new LoggerDisposer(projectManager, this)) {
                        projectManager.UpdatePackageReference(Id, Version, UpdateDependencies.IsPresent);
                    }
                }
                else {
                    using (new LoggerDisposer(packageManager, this)) {
                        // if there is no project specified, update at the solution level
                        packageManager.UpdatePackage(Id, Version, UpdateDependencies);
                    }
                }
            }
        }
    }
}
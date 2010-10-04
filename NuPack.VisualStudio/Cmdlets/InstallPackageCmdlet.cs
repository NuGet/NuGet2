using System;
using System.Globalization;
using System.Management.Automation;

namespace NuPack.VisualStudio.Cmdlets {
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCmdlet : ProcessPackageBaseCmdlet {
        #region Parameters

        [Parameter(Position = 2)]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter IgnoreDependencies { get; set; }

        #endregion

        protected override void ProcessRecordCore() {
            if (!IsSolutionOpen) {
                WriteError("There is no active solution in the current environment.");
                return;
            }

            var packageManager = PackageManager;
            bool isSolutionLevelPackage = IsSolutionOnlyPackage(packageManager.SourceRepository, Id, Version);

            if (isSolutionLevelPackage) {
                if (!String.IsNullOrEmpty(Project)) {
                    WriteError(String.Format(
                        CultureInfo.CurrentCulture,
                        "The package '{0}' only applies to the solution and not to a project. Remove the -Project parameter.",
                        Id));
                }
                else {
                    packageManager.InstallPackage(Id, Version, IgnoreDependencies.IsPresent);
                }
            }
            else {
                var projectManager = ProjectManager;
                if (projectManager != null) {
                    projectManager.AddPackageReference(Id, Version, IgnoreDependencies.IsPresent);
                }
                else {
                    WriteError("Missing project parameter or invalid project name.");
                }
            }
        }
    }
}
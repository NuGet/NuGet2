using System;
using System.Globalization;
using System.Management.Automation;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This command uninstalls the specified package from the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Uninstall, "Package")]
    public class UninstallPackageCmdlet : ProcessPackageBaseCmdlet {
        [Parameter(Position = 2)]
        public SwitchParameter Force { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter RemoveDependencies { get; set; }

        protected override void ProcessRecordCore() {
            if (!IsSolutionOpen) {
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            var packageManager = PackageManager;
            using (new LoggerDisposer(packageManager.FileSystem, this)) {
                bool isSolutionLevel = IsSolutionOnlyPackage(packageManager.LocalRepository, Id);
                if (isSolutionLevel) {
                    if (!String.IsNullOrEmpty(Project)) {
                        WriteError(String.Format(
                            CultureInfo.CurrentCulture,
                            VsResources.Cmdlet_PackageForSolutionOnly,
                            Id));
                    }
                    else {
                        using (new LoggerDisposer(packageManager, this)) {
                            packageManager.UninstallPackage(Id, null, Force.IsPresent, RemoveDependencies.IsPresent);
                        }
                    }
                }
                else {
                    var projectManager = ProjectManager;
                    if (projectManager != null) {
                        using (new LoggerDisposer(projectManager, this)) {
                            projectManager.RemovePackageReference(Id, Force.IsPresent, RemoveDependencies.IsPresent);
                        }
                    }
                    else {
                        WriteError(VsResources.Cmdlet_MissingProjectParameter);
                    }
                }
            }
        }
    }
}

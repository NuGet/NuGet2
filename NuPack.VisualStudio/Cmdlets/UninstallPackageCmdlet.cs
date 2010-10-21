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
            EnvDTE.Project project = GetProjectFromName(Project ?? DefaultProjectName);
            packageManager.UninstallPackage(project, Id, Force.IsPresent, RemoveDependencies.IsPresent, this);
        }
    }
}

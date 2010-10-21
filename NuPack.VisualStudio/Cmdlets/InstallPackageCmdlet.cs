using System;
using System.Management.Automation;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCmdlet : ProcessPackageBaseCmdlet {

        [Parameter(Position = 2)]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter(Position = 4)]
        public string Source { get; set; }

        protected override void ProcessRecordCore() {
            if (!IsSolutionOpen) {
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            if (!String.IsNullOrEmpty(Source)) {
                PackageManager = GetPackageManager(Source);
            }

            ProjectManager projectManager = ProjectManager;
            PackageManager.InstallPackage(projectManager, Id, Version, IgnoreDependencies.IsPresent, this);
        }
    }
}
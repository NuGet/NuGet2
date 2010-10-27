using System;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio.Cmdlets {

    /// <summary>
    /// This command uninstalls the specified package from the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Uninstall, "Package")]
    public class UninstallPackageCmdlet : ProcessPackageBaseCmdlet {

        public UninstallPackageCmdlet()
            : this(NuGet.VisualStudio.SolutionManager.Current, DefaultVsPackageManagerFactory.Instance) {
        }

        public UninstallPackageCmdlet(ISolutionManager solutionManager, IVsPackageManagerFactory packageManagerFactory)
            : base(solutionManager, packageManagerFactory) {
        }

        [Parameter(Position = 2)]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter Force { get; set; }

        [Parameter(Position = 4)]
        public SwitchParameter RemoveDependencies { get; set; }

        protected override void ProcessRecordCore() {
            if (!SolutionManager.IsSolutionOpen) {
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            IProjectManager projectManager = ProjectManager;
            PackageManager.UninstallPackage(projectManager, Id, Version, Force.IsPresent, RemoveDependencies.IsPresent, this);
        }
    }
}

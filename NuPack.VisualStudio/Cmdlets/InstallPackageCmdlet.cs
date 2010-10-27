using System;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio.Cmdlets {
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCmdlet : ProcessPackageBaseCmdlet {

        public InstallPackageCmdlet()
            : this(NuGet.VisualStudio.SolutionManager.Current, DefaultVsPackageManagerFactory.Instance) {
        }

        public InstallPackageCmdlet(ISolutionManager solutionManager, IVsPackageManagerFactory packageManagerFactory)
            : base(solutionManager, packageManagerFactory) {
        }

        [Parameter(Position = 2)]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter(Position = 4)]
        public string Source { get; set; }

        protected override IVsPackageManager  CreatePackageManager() {
            if (!SolutionManager.IsSolutionOpen) {
                return null;
            }

            if (!String.IsNullOrEmpty(Source)) {
                return PackageManagerFactory.CreatePackageManager(Source);
            }

            return base.CreatePackageManager();
        }

        protected override void ProcessRecordCore() {
            if (!SolutionManager.IsSolutionOpen) {
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            IProjectManager projectManager = ProjectManager;
            PackageManager.InstallPackage(projectManager, Id, Version, IgnoreDependencies.IsPresent, this);
        }
    }
}

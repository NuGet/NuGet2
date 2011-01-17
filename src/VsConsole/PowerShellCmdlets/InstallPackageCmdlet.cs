using System;
using System.Management.Automation;

using NuGet.VisualStudio;

namespace NuGet.Cmdlets {
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCmdlet : ProcessPackageBaseCmdlet {

        public InstallPackageCmdlet()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>()) {
        }

        public InstallPackageCmdlet(ISolutionManager solutionManager, IVsPackageManagerFactory packageManagerFactory)
            : base(solutionManager, packageManagerFactory) {
        }

        [Parameter(Position = 2)]
        [ValidateNotNull]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        public SwitchParameter IgnoreDependencies { get; set; }

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
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            IProjectManager projectManager = ProjectManager;
            PackageManager.InstallPackage(projectManager, Id, Version, IgnoreDependencies.IsPresent, this);
        }
    }
}

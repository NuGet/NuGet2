using System;
using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.Cmdlets {

    /// <summary>
    /// This project updates the specfied package to the specfied project.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Package")]
    public class UpdatePackageCmdlet : ProcessPackageBaseCmdlet {

        public UpdatePackageCmdlet()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>()) {
        }

        public UpdatePackageCmdlet(ISolutionManager solutionManager, IVsPackageManagerFactory packageManagerFactory)
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

        protected override IVsPackageManager CreatePackageManager() {
            if (!String.IsNullOrEmpty(Source)) {
                return PackageManagerFactory.CreatePackageManager(Source);
            }
            return base.CreatePackageManager();
        }

        protected override void ProcessRecordCore() {
            if (!SolutionManager.IsSolutionOpen) {
                // terminating
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            IProjectManager projectManager = ProjectManager;
            PackageManager.UpdatePackage(projectManager, Id, Version, !IgnoreDependencies, this);
        }
    }
}
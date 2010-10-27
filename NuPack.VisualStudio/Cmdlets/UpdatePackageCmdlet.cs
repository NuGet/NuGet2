using System;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio.Cmdlets {

    /// <summary>
    /// This project updates the specfied package to the specfied project.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Package")]
    public class UpdatePackageCmdlet : ProcessPackageBaseCmdlet {

        public UpdatePackageCmdlet()
            : this(NuGet.VisualStudio.SolutionManager.Current, DefaultVsPackageManagerFactory.Instance) {
        }

        public UpdatePackageCmdlet(ISolutionManager solutionManager, IVsPackageManagerFactory packageManagerFactory)
            : base(solutionManager, packageManagerFactory) {
        }

        [Parameter(Position = 2)]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter UpdateDependencies { get; set; }

        [Parameter(Position = 4)]
        public string Source { get; set; }

        protected override IVsPackageManager CreatePackageManager() {
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
            PackageManager.UpdatePackage(projectManager, Id, Version, UpdateDependencies, this);
        }
    }
}

using System;
using System.Management.Automation;
using EnvDTE;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This project updates the specfied package to the specfied project.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Package")]
    public class UpdatePackageCmdlet : ProcessPackageBaseCmdlet {

        public UpdatePackageCmdlet()
            : this(NuPack.VisualStudio.SolutionManager.Current, CachedRepositoryFactory.Instance, DTEExtensions.DTE, packageManager: null) {
        }

        public UpdatePackageCmdlet(ISolutionManager solutionManager, IPackageRepositoryFactory repositoryFactory, DTE dte, VsPackageManager packageManager)
            : base(solutionManager, repositoryFactory, dte) {

            if (packageManager != null) {
                PackageManager = packageManager;
            }
        }

        [Parameter(Position = 2)]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter UpdateDependencies { get; set; }

        [Parameter(Position = 4)]
        public string Source { get; set; }

        protected override void ProcessRecordCore() {
            if (!SolutionManager.IsSolutionOpen) {
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            if (!String.IsNullOrEmpty(Source)) {
                PackageManager = GetPackageManager(Source);
            }

            IProjectManager projectManager = ProjectManager;
            PackageManager.UpdatePackage(projectManager, Id, Version, UpdateDependencies, this);
        }
    }
}
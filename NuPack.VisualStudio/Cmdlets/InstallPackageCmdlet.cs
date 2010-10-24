using System;
using System.Management.Automation;
using EnvDTE;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCmdlet : ProcessPackageBaseCmdlet {

        public InstallPackageCmdlet()
            : this(NuPack.VisualStudio.SolutionManager.Current, CachedRepositoryFactory.Instance, DTEExtensions.DTE, packageManager: null) {
        }

        public InstallPackageCmdlet(ISolutionManager solutionManager, IPackageRepositoryFactory repositoryFactory, DTE dte, VsPackageManager packageManager)
            : base(solutionManager, repositoryFactory, dte) {

            if (packageManager != null) {
                PackageManager = packageManager;
            }
        }

        [Parameter(Position = 2)]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        public SwitchParameter IgnoreDependencies { get; set; }

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
            PackageManager.InstallPackage(projectManager, Id, Version, IgnoreDependencies.IsPresent, this);
        }
    }
}
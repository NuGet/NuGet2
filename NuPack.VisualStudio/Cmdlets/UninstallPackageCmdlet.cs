using System;
using System.Management.Automation;
using EnvDTE;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This command uninstalls the specified package from the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Uninstall, "Package")]
    public class UninstallPackageCmdlet : ProcessPackageBaseCmdlet {

        public UninstallPackageCmdlet()
            : this(NuPack.VisualStudio.SolutionManager.Current, CachedRepositoryFactory.Instance, DTEExtensions.DTE, packageManager: null) {
        }

        public UninstallPackageCmdlet(ISolutionManager solutionManager, IPackageRepositoryFactory repositoryFactory, DTE dte, VsPackageManager packageManager)
            : base(solutionManager, repositoryFactory, dte) {

            if (packageManager != null) {
                PackageManager = packageManager;
            }
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

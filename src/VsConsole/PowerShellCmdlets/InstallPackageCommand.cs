using System;
using System.Management.Automation;

using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands {
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCommand : ProcessPackageBaseCommand {

        private readonly IProductUpdateService _productUpdateService;

        public InstallPackageCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(), 
                   ServiceLocator.GetInstance<IProgressProvider>(),
                   ServiceLocator.GetInstance<IProductUpdateService>()) {
        }

        public InstallPackageCommand(
            ISolutionManager solutionManager, 
            IVsPackageManagerFactory packageManagerFactory, 
            IProgressProvider progressProvider,
            IProductUpdateService productUpdateService)
            : base(solutionManager, packageManagerFactory, progressProvider) {
            _productUpdateService = productUpdateService;
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

            try {
                SubscribeToProgressEvents();
                IProjectManager projectManager = ProjectManager;
                PackageManager.InstallPackage(projectManager, Id, Version, IgnoreDependencies.IsPresent, this);
            }
            finally {
                UnsubscribeFromProgressEvents();
            }
        }

        protected override void EndProcessing() {
            base.EndProcessing();

            CheckForNuGetUpdate();
        }

        protected void CheckForNuGetUpdate() {
            if (_productUpdateService != null) {
                _productUpdateService.CheckForAvailableUpdateAsync();
            }
        }
    }
}
using System;
using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands {

    /// <summary>
    /// This project updates the specfied package to the specfied project.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Package")]
    public class UpdatePackageCommand : ProcessPackageBaseCommand {

        private readonly IProductUpdateService _productUpdateService;
        private bool _hasConnectedToHttpSource;

        public UpdatePackageCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>(),
                   ServiceLocator.GetInstance<IProductUpdateService>()) {
        }

        public UpdatePackageCommand(ISolutionManager solutionManager, 
                                    IVsPackageManagerFactory packageManagerFactory, 
                                    IHttpClientEvents httpClientEvents,
                                    IProductUpdateService productUpdateService)
            : base(solutionManager, packageManagerFactory, httpClientEvents) {
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

            try {
                SubscribeToProgressEvents();
                IProjectManager projectManager = ProjectManager;
                if (PackageManager != null) {
                    PackageManager.UpdatePackage(projectManager, Id, Version, !IgnoreDependencies, this);
                    _hasConnectedToHttpSource |= UriHelper.IsHttpSource(PackageManager.SourceRepository.Source);
                }
            }
            finally {
                UnsubscribeFromProgressEvents();
            }
        }

        protected override void EndProcessing() {
            base.EndProcessing();

            CheckForNuGetUpdate();
        }

        private void CheckForNuGetUpdate() {
            if (_productUpdateService != null && _hasConnectedToHttpSource) {
                _productUpdateService.CheckForAvailableUpdateAsync();
            }
        }
    }
}
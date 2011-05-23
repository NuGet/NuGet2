using System;
using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands {
    /// <summary>
    /// This project updates the specfied package to the specfied project.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Package", DefaultParameterSetName = "All")]
    public class UpdatePackageCommand : ProcessPackageBaseCommand {
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IProductUpdateService _productUpdateService;
        private bool _hasConnectedToHttpSource;

        public UpdatePackageCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>(),
                   ServiceLocator.GetInstance<IProductUpdateService>()) {
        }

        public UpdatePackageCommand(ISolutionManager solutionManager,
                                    IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepositoryFactory repositoryFactory,
                                    IVsPackageSourceProvider packageSourceProvider,
                                    IHttpClientEvents httpClientEvents,
                                    IProductUpdateService productUpdateService)
            : base(solutionManager, packageManagerFactory, httpClientEvents) {
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            _productUpdateService = productUpdateService;
        }

        // We need to override id since it's mandatory in the base class. We don't
        // want it to be mandatory here.
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Project")]
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "All")]
        public override string Id {
            get {
                return base.Id;
            }
            set {
                base.Id = value;
            }
        }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "All")]
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "Project")]
        public override string ProjectName {
            get {
                return base.ProjectName;
            }
            set {
                base.ProjectName = value;
            }
        }

        [Parameter(Position = 2, ParameterSetName = "Project")]
        [ValidateNotNull]
        public Version Version { get; set; }

        [Parameter(Position = 3)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter]
        public SwitchParameter Safe { get; set; }

        protected override IVsPackageManager CreatePackageManager() {
            if (!String.IsNullOrEmpty(Source)) {
                IPackageRepository repository = CreateRepositoryFromSource(_repositoryFactory, _packageSourceProvider, Source);
                return PackageManagerFactory.CreatePackageManager(repository);
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
                if (PackageManager != null) {
                    IProjectManager projectManager = ProjectManager;
                    if (!String.IsNullOrEmpty(Id)) {
                        // If a package id was specified, but no project was specified, then update this package in all projects
                        if (String.IsNullOrEmpty(ProjectName)) {
                            if (Safe.IsPresent) {
                                PackageManager.SafeUpdatePackage(Id, !IgnoreDependencies.IsPresent, this);
                            }
                            else {
                                PackageManager.UpdatePackage(Id, Version, !IgnoreDependencies.IsPresent, this);
                            }
                        }
                        else if (projectManager != null) {
                            // If there was a project specified, then update the package in that project
                            if (Safe.IsPresent) {
                                PackageManager.SafeUpdatePackage(projectManager, Id, !IgnoreDependencies, this);
                            }
                            else {
                                PackageManager.UpdatePackage(projectManager, Id, Version, !IgnoreDependencies, this);
                            }
                        }
                    }
                    else {
                        // if no id was specified then update all packges in the solution
                        if (Safe.IsPresent) {
                            PackageManager.SafeUpdatePackages(this);
                        }
                        else {
                            PackageManager.UpdatePackages(this);
                        }
                    }
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
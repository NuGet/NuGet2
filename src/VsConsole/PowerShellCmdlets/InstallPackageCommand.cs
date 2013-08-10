using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCommand : ProcessPackageBaseCommand
    {
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IProductUpdateService _productUpdateService;
        private bool _hasConnectedToHttpSource;

        public InstallPackageCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>(),
                   ServiceLocator.GetInstance<IProductUpdateService>(),
                   ServiceLocator.GetInstance<IVsCommonOperations>(),
                   ServiceLocator.GetInstance<IDeleteOnRestartManager>())
        {
        }

        public InstallPackageCommand(
            ISolutionManager solutionManager,
            IVsPackageManagerFactory packageManagerFactory,
            IPackageRepositoryFactory repositoryFactory,
            IVsPackageSourceProvider packageSourceProvider,
            IHttpClientEvents httpClientEvents,
            IProductUpdateService productUpdateService,
            IVsCommonOperations vsCommonOperations,
            IDeleteOnRestartManager deleteOnRestartManager)
            : base(solutionManager, packageManagerFactory, httpClientEvents, vsCommonOperations, deleteOnRestartManager)
        {
            _productUpdateService = productUpdateService;
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        [Parameter(Position = 2)]
        [ValidateNotNull]
        public SemanticVersion Version { get; set; }

        [Parameter(Position = 3)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        public FileConflictAction FileConflictAction { get; set; }

        [Parameter]
        public SwitchParameter AcceptLicenses { get; set; }

        protected override IVsPackageManager CreatePackageManager()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                return null;
            }

            if (!String.IsNullOrEmpty(Source))
            {
                var repository = CreateRepositoryFromSource(_repositoryFactory, _packageSourceProvider, Source);
                return repository == null ? null : PackageManagerFactory.CreatePackageManager(repository, useFallbackForDependencies: true);
            }

            return base.CreatePackageManager();
        }

        protected override void ProcessRecordCore()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            try
            {
                SubscribeToProgressEvents();
                if (PackageManager != null)
                {
                    if (AcceptLicenses.IsPresent)
                    {
                        PackageManager.InstallPackage(ProjectManager, Id, Version, IgnoreDependencies, IncludePrerelease.IsPresent, logger: this);
                        
                    }
                    else
                    {
                        ICollection<IPackage> licensePackages = GetLicensePackages();
                        bool accepted = AskForLicenseAcceptance(licensePackages);
                        if (!accepted)
                        {
                            return;
                        }

                        //PackageManager.InstallPackage()
                    }

                    _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source, _packageSourceProvider);
                }
            }
            finally
            {
                UnsubscribeFromProgressEvents();
            }
        }

        public ICollection<IPackage> GetLicensePackages()
        {
            var package = PackageManager.LocalRepository.FindPackage(Id, Version);
            if (package == null)
            {
                package = PackageManager.SourceRepository.FindPackage(Id, Version);
            }

            if (package == null)
            {
                throw new PackageNotInstalledException();
            }

            var walker = new InstallWalker(
                ProjectManager.LocalRepository,
                PackageManager.SourceRepository,
                GetProjectTargetFramework(),
                this,
                IgnoreDependencies.IsPresent,
                IncludePrerelease.IsPresent);

            IList<PackageOperation> operations = walker.ResolveOperations(package).ToArray();

            var licensePackages = from o in operations
                                  where o.Action == PackageAction.Install &&
                                        o.Package.RequireLicenseAcceptance &&
                                        !PackageManager.LocalRepository.Exists(o.Package)
                                  select o.Package;

            return licensePackages.ToArray();
        }

        public override FileConflictResolution ResolveFileConflict(string message)
        {
            if (FileConflictAction == FileConflictAction.Overwrite)
            {
                return FileConflictResolution.Overwrite;
            }

            if (FileConflictAction == FileConflictAction.Ignore)
            {
                return FileConflictResolution.Ignore;
            }

            return base.ResolveFileConflict(message);
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            CheckForNuGetUpdate();
        }

        private void CheckForNuGetUpdate()
        {
            if (_productUpdateService != null && _hasConnectedToHttpSource)
            {
                _productUpdateService.CheckForAvailableUpdateAsync();
            }
        }
    }
}
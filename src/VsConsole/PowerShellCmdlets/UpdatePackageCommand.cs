using System;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This project updates the specified package to the specified project.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Package", DefaultParameterSetName = "All")]
    public class UpdatePackageCommand : ProcessPackageBaseCommand, IPackageOperationEventListener
    {
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
                   ServiceLocator.GetInstance<IProductUpdateService>(),
                   ServiceLocator.GetInstance<IVsCommonOperations>(),
                   ServiceLocator.GetInstance<IDeleteOnRestartManager>())
        {
        }

        public UpdatePackageCommand(ISolutionManager solutionManager,
                                    IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepositoryFactory repositoryFactory,
                                    IVsPackageSourceProvider packageSourceProvider,
                                    IHttpClientEvents httpClientEvents,
                                    IProductUpdateService productUpdateService,
                                    IVsCommonOperations vsCommonOperations,
                                    IDeleteOnRestartManager deleteOnRestartManager)
            : base(solutionManager, packageManagerFactory, httpClientEvents, vsCommonOperations, deleteOnRestartManager)
        {
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            _productUpdateService = productUpdateService;
        }

        // We need to override id since it's mandatory in the base class. We don't
        // want it to be mandatory here.
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Project")]
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "All")]
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Reinstall")]
        public override string Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "All")]
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "Project")]
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "Reinstall")]
        public override string ProjectName
        {
            get
            {
                return base.ProjectName;
            }
            set
            {
                base.ProjectName = value;
            }
        }

        [Parameter(Position = 2, ParameterSetName = "Project")]
        [ValidateNotNull]
        public SemanticVersion Version { get; set; }

        [Parameter(Position = 3)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter]
        public SwitchParameter Safe { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Reinstall")]
        [Parameter(ParameterSetName = "All")]
        public SwitchParameter Reinstall { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        public FileConflictAction FileConflictAction { get; set; }

        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        protected override IVsPackageManager CreatePackageManager()
        {
            if (!String.IsNullOrEmpty(Source))
            {
                IPackageRepository repository = CreateRepositoryFromSource(_repositoryFactory, _packageSourceProvider, Source);
                return repository == null ? null : PackageManagerFactory.CreatePackageManager(repository, useFallbackForDependencies: true);
            }
            return base.CreatePackageManager();
        }

        protected override void ProcessRecordCore()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                // terminating
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            if (WhatIf && Reinstall)
            {
                Logger.Log(MessageLevel.Error, Resources.Cmdlet_WhatIfReinstallUnsupported);
                return;
            }

            try
            {
                SubscribeToProgressEvents();
                if (PackageManager != null)
                {
                    PackageManager.WhatIf = WhatIf;
                    if (ProjectManager != null)
                    {
                        ProjectManager.WhatIf = WhatIf;
                    }

                    if (Reinstall)
                    {
                        PerformReinstalls(ProjectManager);
                    }
                    else
                    {
                        PerformUpdates(ProjectManager);
                    }
                    _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source, _packageSourceProvider);
                }
            }
            finally
            {
                UnsubscribeFromProgressEvents();
            }
        }

        private void PerformReinstalls(IProjectManager projectManager)
        {
            if (!String.IsNullOrEmpty(Id))
            {
                // If a package id was specified, but no project was specified, then update this package in all projects
                if (String.IsNullOrEmpty(ProjectName))
                {
                    PackageManager.ReinstallPackage(Id, !IgnoreDependencies, IncludePrerelease, Logger, this);
                }
                else if (projectManager != null)
                {
                    PackageManager.ReinstallPackage(projectManager, Id, !IgnoreDependencies, IncludePrerelease, Logger);
                }
            }
            else
            {
                if (String.IsNullOrEmpty(ProjectName))
                {
                    PackageManager.ReinstallPackages(!IgnoreDependencies, IncludePrerelease, Logger, this);
                }
                else if (projectManager != null)
                {
                    PackageManager.ReinstallPackages(projectManager, !IgnoreDependencies, IncludePrerelease, Logger);
                }
            }
        }

        private void PerformUpdates(IProjectManager projectManager)
        {
            if (!String.IsNullOrEmpty(Id))
            {
                // If a package id was specified, but no project was specified, then update this package in all projects
                if (String.IsNullOrEmpty(ProjectName))
                {
                    if (Safe.IsPresent)
                    {
                        PackageManager.SafeUpdatePackage(Id, !IgnoreDependencies.IsPresent, IncludePrerelease, Logger, this);
                    }
                    else
                    {
                        PackageManager.UpdatePackage(Id, Version, !IgnoreDependencies.IsPresent, IncludePrerelease, Logger, this);
                    }
                }
                else if (projectManager != null)
                {
                    // If there was a project specified, then update the package in that project
                    if (Safe.IsPresent)
                    {
                        PackageManager.SafeUpdatePackage(projectManager, Id, !IgnoreDependencies, IncludePrerelease, Logger);
                    }
                    else
                    {
                        PackageManager.UpdatePackage(projectManager, Id, Version, !IgnoreDependencies, IncludePrerelease, Logger);
                    }
                }
            }
            else
            {
                // if no id was specified then update all packages in the solution
                if (Safe.IsPresent)
                {
                    if (String.IsNullOrEmpty(ProjectName))
                    {
                        PackageManager.SafeUpdatePackages(!IgnoreDependencies.IsPresent, IncludePrerelease, Logger, this);
                    }
                    else if (projectManager != null)
                    {
                        PackageManager.SafeUpdatePackages(projectManager, !IgnoreDependencies.IsPresent, IncludePrerelease, Logger);
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(ProjectName))
                    {
                        PackageManager.UpdatePackages(!IgnoreDependencies.IsPresent, IncludePrerelease, Logger, this);
                    }
                    else if (projectManager != null)
                    {
                        PackageManager.UpdatePackages(projectManager, !IgnoreDependencies.IsPresent, IncludePrerelease, Logger);
                    }
                }
            }
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

        public void OnBeforeAddPackageReference(Project project)
        {
            RegisterProjectEvents(project);
        }

        public void OnAfterAddPackageReference(Project project)
        {
            // No-op
        }

        public void OnAddPackageReferenceError(Project project, Exception exception)
        {
            // No-op
        }
    }
}
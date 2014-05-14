using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using EnvDTE;
using NuGet.Resolver;
using NuGet.Resources;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;

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
        
        private OperationResolver _resolver;
        private OperationExecutor _operationExecutor;

        protected override void ProcessRecordCore()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                // terminating
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            try
            {
                SubscribeToProgressEvents();
                if (PackageManager != null)
                {
                    _resolver = new OperationResolver(PackageManager)
                    {
                        Logger = this,
                        DependencyVersion = PackageManager.DependencyVersion,
                        IgnoreDependencies = IgnoreDependencies,
                        AllowPrereleaseVersions = IncludePrerelease.IsPresent
                    };

                    _operationExecutor = new OperationExecutor()
                    {
                        Logger = this,
                        PackageOperationEventListener = this,
                        CatchProjectOperationException = true
                    };
            
                    if (Reinstall)
                    {
                        PerformReinstalls();
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

        private void PerformReinstalls()
        {
            if (String.IsNullOrEmpty(ProjectName))
            {
                var projectManagers = PackageManager.SolutionManager.GetProjects()
                    .Select(p => PackageManager.GetProjectManager(p));
                ReinstallPackage(projectManagers);
            }
            else if (ProjectManager != null)
            {
                ReinstallPackage(new[] { ProjectManager });
            }
        }

        /// <summary>
        /// Reinstall package Id, Version in the list of projects.
        /// </summary>
        /// <param name="projectManagers">The list of project managers.</param>
        /// <param name="projectNameSpecified">Indicates if the project name is specified when 
        /// Update-Package -reinstall cmdlet is executed.</param>
        private void ReinstallPackage(IEnumerable<IProjectManager> projectManagers)
        {
            if (String.IsNullOrEmpty(Id))
            {
                ReinstallAllPackages(projectManagers);
            }
            else
            {
                ReinstallOnePackage(Id, projectManagers);
            }
        }

        private void ReinstallAllPackages(IEnumerable<IProjectManager> projectManagers)
        {
            var virtualProjectManagers = projectManagers.Select(p => new VirtualProjectManager(p)).ToList();
            var packages = PackageManager.LocalRepository.GetPackages().ToList();
            var solutionLevelPackages = packages.Where(p => !PackageManager.IsProjectLevel(p));

            // reinstall solution level packages
            var solutionLevelOperations = new List<Operation>();
            foreach (var package in solutionLevelPackages)
            {
                solutionLevelOperations.Add(
                    new Operation(
                        new PackageOperation(package, PackageAction.Uninstall)
                        {
                            Target = PackageOperationTarget.PackagesFolder
                        },
                        projectManager: null,
                        packageManager: PackageManager));
            }

            var operations = new List<Operation>(solutionLevelOperations);

            // Add reverse operations
            for (int i = solutionLevelOperations.Count - 1; i >= 0; --i)
            {
                var operation = solutionLevelOperations[i];
                var package = PackageManager.SourceRepository.FindPackage(
                    operation.Package.Id,
                    operation.Package.Version);

                var reverseOp = new Operation(
                    new PackageOperation(
                        package,
                        operation.Action == PackageAction.Install ?
                        PackageAction.Uninstall :
                        PackageAction.Install)
                    {
                        Target = operation.Target
                    },
                    projectManager: operation.ProjectManager,
                    packageManager: operation.PackageManager);
                operations.Add(reverseOp);
            }

            // Reinstall packages in projects
            foreach (var projectManager in virtualProjectManagers)
            {
                var ops = GetOperationsToReninstallAllPackages(projectManager);
                operations.AddRange(ops);
            }

            if (WhatIf)
            {
                foreach (var operation in operations)
                {
                    Log(MessageLevel.Info, Resources.Log_OperationWhatIf, operation);
                }

                return;
            }

            _operationExecutor.Execute(operations);
        }

        private void UninstallOneProjectLevelPackage(string id, ReinstallInfo reinstallInfo)
        {
            foreach (var projectManager in reinstallInfo.VirtualProjectManagers)
            {
                // find the package version installed in this project
                IPackage existingPackage = projectManager.ProjectManager.LocalRepository.FindPackage(id);
                if (existingPackage == null)
                {
                    continue;
                }

                bool packageExistInSource;
                if (!reinstallInfo.VersionsChecked.TryGetValue(existingPackage.Version, out packageExistInSource))
                {
                    // version has not been checked, so check it here
                    packageExistInSource = PackageManager.SourceRepository.Exists(id, existingPackage.Version);

                    // mark the version as checked so that we don't have to check again if we
                    // encounter another project with the same version.
                    reinstallInfo.VersionsChecked[existingPackage.Version] = packageExistInSource;
                }

                if (packageExistInSource)
                {
                    reinstallInfo.PackagesInProject[projectManager] = existingPackage;
                    var oldValue = _resolver.ForceRemove;
                    try
                    {
                        _resolver.ForceRemove = true;
                        var projectOps = _resolver.ResolveProjectOperations(
                            UserOperation.Uninstall,
                            existingPackage,
                            projectManager);
                        reinstallInfo.ProjectOperations.AddRange(projectOps);
                    }
                    finally
                    {
                        _resolver.ForceRemove = oldValue;
                    }
                }
                else
                {
                    Log(
                        MessageLevel.Warning,
                        VsResources.PackageRestoreSkipForProject,
                        existingPackage.GetFullName(),
                        projectManager.ProjectManager.Project.ProjectName);
                }
            }
        }

        private void ReinstallPackage(ReinstallInfo reinstallInfo)
        {
            foreach (var projectManager in reinstallInfo.VirtualProjectManagers)
            {
                IPackage package;
                if (!reinstallInfo.PackagesInProject.TryGetValue(projectManager, out package))
                {
                    continue;
                }

                var projectOps = _resolver.ResolveProjectOperations(
                    UserOperation.Install,
                    package,
                    projectManager);
                reinstallInfo.ProjectOperations.AddRange(projectOps);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGet.ILogger.Log(NuGet.MessageLevel,System.String,System.Object[])")]
        private void ReinstallOnePackage(string id, IEnumerable<IProjectManager> projectManagers)
        {
            List<Operation> operations = new List<Operation>();
            var projectNameSpecified = !String.IsNullOrEmpty(ProjectName);
            var oldPackage = projectNameSpecified ?
                UpdateUtility.FindPackageToUpdate(
                    id, version: null, 
                    packageManager: PackageManager,
                    projectManager: projectManagers.First()) :
                UpdateUtility.FindPackageToUpdate(
                    id, version: null, 
                    packageManager: PackageManager,
                    projectManagers: projectManagers, 
                    logger: this);

            if (oldPackage.Item2 == null)
            {
                // we're reinstalling a solution level package
                Log(MessageLevel.Info, VsResources.ReinstallSolutionPackage, oldPackage.Item1);
                if (PackageManager.SourceRepository.Exists(oldPackage.Item1))
                {
                    var solutionOps = _resolver.ResolveProjectOperations(
                        UserOperation.Uninstall,
                        oldPackage.Item1, 
                        projectManager: null);
                    operations.AddRange(solutionOps);

                    // Add reverse operations
                    for (int i = solutionOps.Count - 1; i >= 0; --i)
                    {
                        var operation = solutionOps[i];
                        var package = PackageManager.SourceRepository.FindPackage(
                            operation.Package.Id,
                            operation.Package.Version);

                        var reverseOp = new Operation(
                            new PackageOperation(
                                package,
                                operation.Action == PackageAction.Install ?
                                PackageAction.Uninstall :
                                PackageAction.Install)
                            {
                                Target = operation.Target
                            },
                            projectManager: operation.ProjectManager,
                            packageManager: operation.PackageManager);
                        operations.Add(reverseOp);
                    }
                }
                else
                {
                    Log(
                        MessageLevel.Warning,
                        VsResources.PackageRestoreSkipForSolution,
                        oldPackage.Item1.GetFullName());
                }
            }
            else
            {
                var reinstallInfo = new ReinstallInfo(projectManagers.Select(p => new VirtualProjectManager(p)));
                UninstallOneProjectLevelPackage(id, reinstallInfo);
                ReinstallPackage(reinstallInfo);
                operations = reinstallInfo.ProjectOperations;
            }

            if (WhatIf)
            {
                foreach (var operation in operations)
                {
                    Log(MessageLevel.Info, Resources.Log_OperationWhatIf, operation);
                }

                return;
            }

            _operationExecutor.Execute(operations);
        }

        // Reinstall all packages in a project
        private List<Operation> GetOperationsToReninstallAllPackages(VirtualProjectManager projectManager)
        {
            List<Operation> operations = new List<Operation>();
            var packages = projectManager.LocalRepository.GetPackages().ToList();

            // Uninstall all packages
            foreach (var p in packages)
            {
                operations.Add(new Operation(
                    new PackageOperation(p, PackageAction.Uninstall)
                    {
                        Target = PackageOperationTarget.Project
                    },
                    projectManager: projectManager.ProjectManager,
                    packageManager: null));
                projectManager.LocalRepository.RemovePackage(p);
            }

            // Install those packages back
            foreach (var package in packages)
            {
                var ops = _resolver.ResolveProjectOperations(
                    UserOperation.Install, 
                    package, 
                    projectManager);
                operations.AddRange(ops);
            }

            return operations;
        }

        private void PerformUpdates(IProjectManager projectManager)
        {
            var updateUtility = new UpdateUtility(_resolver)
            {
                AllowPrereleaseVersions = IncludePrerelease.IsPresent,
                Logger = this,
                Safe = Safe.IsPresent
            };
            var operations = Enumerable.Empty<Operation>();
            if (String.IsNullOrEmpty(ProjectName))
            {
                var projectManagers = PackageManager.SolutionManager.GetProjects()
                    .Select(p => PackageManager.GetProjectManager(p));
                operations = updateUtility.ResolveOperationsForUpdate(
                    Id, Version, projectManagers, projectNameSpecified: false);
            }
            else if (projectManager != null)
            {
                operations = updateUtility.ResolveOperationsForUpdate(
                    Id, Version, new[] { projectManager }, projectNameSpecified: true);
            }

            if (WhatIf)
            {
                foreach (var operation in operations)
                {
                    Log(MessageLevel.Info, Resources.Log_OperationWhatIf, operation);
                }

                return;
            }

            _operationExecutor.Execute(operations);
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

        public void OnBeforeAddPackageReference(IProjectManager projectManager)
        {
            var projectSystem = projectManager.Project as VsProjectSystem;
            if (projectSystem != null)
            {
                RegisterProjectEvents(projectSystem.Project);
            }
        }

        public void OnAfterAddPackageReference(IProjectManager projectManager)
        {
            // No-op
        }

        public void OnAddPackageReferenceError(IProjectManager projectManager, Exception exception)
        {
            // No-op
        }
    }
}
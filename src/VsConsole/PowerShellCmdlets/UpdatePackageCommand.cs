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
        
        private ActionResolver _resolver;
        private ActionExecutor _actionExecutor;

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
                    _resolver = new ActionResolver()
                    {
                        Logger = this,
                        DependencyVersion = PackageManager.DependencyVersion,
                        IgnoreDependencies = IgnoreDependencies,
                        AllowPrereleaseVersions = IncludePrerelease.IsPresent,
                        ForceRemove = true
                    };

                    _actionExecutor = new ActionExecutor()
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
            var packages = PackageManager.LocalRepository.GetPackages().ToList();
            var solutionLevelPackages = packages.Where(p => !PackageManager.IsProjectLevel(p));

            var resolver = new ActionResolver()
            {
                ForceRemove = true
            };

            // reinstall solution level packages
            foreach (var package in solutionLevelPackages)
            {
                resolver.AddOperation(PackageAction.Uninstall, package, new NullProjectManager(PackageManager));                
                var packageFromSource = PackageManager.SourceRepository.FindPackage(
                    package.Id,
                    package.Version);
                resolver.AddOperation(PackageAction.Install, packageFromSource, new NullProjectManager(PackageManager));
            }

            // Reinstall packages in projects
            foreach (var projectManager in projectManagers)
            {
                ReinstallAllPackagesInProject(projectManager, resolver);
            }

            var actions = resolver.ResolveActions();
            if (WhatIf)
            {
                foreach (var action in actions)
                {
                    Log(MessageLevel.Info, Resources.Log_OperationWhatIf, action);
                }

                return;
            }

            var executor = new ActionExecutor()
            {
                Logger = this,
                PackageOperationEventListener = this,
                CatchProjectOperationException = true
            };
            executor.Execute(actions);
        }

        private void ReinstallAllPackagesInProject(IProjectManager projectManager, ActionResolver resolver)
        {
            var packages = projectManager.LocalRepository.GetPackages().ToList();

            // Uninstall all packages
            var packagesToInstall = new List<IPackage>();
            foreach (var package in packages)
            {
                var packageFromSource = projectManager.PackageManager.SourceRepository.FindPackage(
                    package.Id,
                    package.Version);

                if (packageFromSource != null)
                {
                    resolver.AddOperation(PackageAction.Uninstall, package, projectManager);
                    packagesToInstall.Add(packageFromSource);
                }
                else
                {
                    Log(
                        MessageLevel.Warning,
                        VsResources.PackageRestoreSkipForProject,
                        package.GetFullName(),
                        projectManager.Project.ProjectName);
                }
            }

            foreach (var package in packagesToInstall)
            {
                resolver.AddOperation(PackageAction.Install, package, projectManager);
            }
        }

        private void ReinstallOnePackage(string id, IEnumerable<IProjectManager> projectManagers)
        {
            List<Resolver.PackageAction> actions = new List<Resolver.PackageAction>();
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
                    var resolver = new ActionResolver()
                    {
                        ForceRemove = true
                    };
                    resolver.AddOperation(PackageAction.Uninstall, oldPackage.Item1, new NullProjectManager(PackageManager));

                    var packageFromSource = PackageManager.SourceRepository.FindPackage(
                        oldPackage.Item1.Id,
                        oldPackage.Item1.Version);
                    resolver.AddOperation(PackageAction.Install, packageFromSource, new NullProjectManager(PackageManager));
                    actions.AddRange(resolver.ResolveActions());
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
                var reinstallInfo = new ReinstallInfo(Enumerable.Empty<VirtualProjectManager>());
                var resolver = new ActionResolver()
                {
                    ForceRemove = true
                };
                foreach (var projectManager in projectManagers)
                {
                    ReinstallPackage(id, projectManager, reinstallInfo, resolver);
                }

                actions.AddRange(resolver.ResolveActions());
            }

            if (WhatIf)
            {
                foreach (var action in actions)
                {
                    Log(MessageLevel.Info, Resources.Log_OperationWhatIf, action);
                }

                return;
            }

            var executor = new ActionExecutor()
            {
                Logger = this,
                PackageOperationEventListener = this,
                CatchProjectOperationException = true
            };
            executor.Execute(actions);
        }

        private void ReinstallPackage(
            string id, IProjectManager projectManager, 
            ReinstallInfo reinstallInfo,
            ActionResolver resolver)
        {
            // find the package version installed in this project
            IPackage existingPackage = projectManager.LocalRepository.FindPackage(id);
            if (existingPackage == null)
            {
                return;
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
                resolver.AddOperation(PackageAction.Uninstall, existingPackage, projectManager);

                var packageFromSource = PackageManager.SourceRepository.FindPackage(id, existingPackage.Version);
                resolver.AddOperation(PackageAction.Install, packageFromSource, projectManager);
            }
            else
            {
                Log(
                    MessageLevel.Warning,
                    VsResources.PackageRestoreSkipForProject,
                    existingPackage.GetFullName(),
                    projectManager.Project.ProjectName);
            }
        }

        private void PerformUpdates(IProjectManager projectManager)
        {
            var updateUtility = new UpdateUtility(_resolver)
            {
                AllowPrereleaseVersions = IncludePrerelease.IsPresent,
                Logger = this,
                Safe = Safe.IsPresent
            };
            var actions = Enumerable.Empty<Resolver.PackageAction>();
            if (String.IsNullOrEmpty(ProjectName))
            {
                var projectManagers = PackageManager.SolutionManager.GetProjects()
                    .Select(p => PackageManager.GetProjectManager(p));
                actions = updateUtility.ResolveActionsForUpdate(
                    Id, Version, projectManagers, projectNameSpecified: false);
            }
            else if (projectManager != null)
            {
                actions = updateUtility.ResolveActionsForUpdate(
                    Id, Version, new[] { projectManager }, projectNameSpecified: true);
            }

            if (WhatIf)
            {
                foreach (var action in actions)
                {
                    Log(MessageLevel.Info, Resources.Log_OperationWhatIf, action);
                }

                return;
            }

            _actionExecutor.Execute(actions);
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
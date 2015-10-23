using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This command lists the available packages which are either from a package source or installed in the current solution.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Package", DefaultParameterSetName = ParameterAttribute.AllParameterSets)]
    [OutputType(typeof(IPackage))]
    public class GetPackageCommand : NuGetBaseCommand
    {
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly IProductUpdateService _productUpdateService;
        private int _firstValue;
        private bool _firstValueSpecified;
        private bool _hasConnectedToHttpSource;

        public GetPackageCommand()
            : this(ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>(),
                   ServiceLocator.GetInstance<IProductUpdateService>())
        {
        }

        public GetPackageCommand(IPackageRepositoryFactory repositoryFactory,
                                IVsPackageSourceProvider packageSourceProvider,
                                ISolutionManager solutionManager,
                                IVsPackageManagerFactory packageManagerFactory,
                                IHttpClientEvents httpClientEvents,
                                IProductUpdateService productUpdateService)
            : base(solutionManager, packageManagerFactory, httpClientEvents)
        {

            if (repositoryFactory == null)
            {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (packageSourceProvider == null)
            {
                throw new ArgumentNullException("packageSourceProvider");
            }

            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            _productUpdateService = productUpdateService;
        }

        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

        [Parameter(Position = 1, ParameterSetName = "Remote")]
        [Parameter(Position = 1, ParameterSetName = "Updates")]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Project")]
        [ValidateNotNullOrEmpty]
        public string ProjectName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Remote")]
        [Alias("Online", "Remote")]
        public SwitchParameter ListAvailable { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Updates")]
        public SwitchParameter Updates { get; set; }

        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "Updates")]
        public SwitchParameter AllVersions { get; set; }

        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "Updates")]
        [Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public int First
        {
            get
            {
                return _firstValue;
            }
            set
            {
                _firstValue = value;
                _firstValueSpecified = true;
            }
        }

        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public int Skip { get; set; }

        /// <summary>
        /// Determines if local repository are not needed to process this command
        /// </summary>
        private bool UseRemoteSourceOnly
        {
            get
            {
                return ListAvailable.IsPresent || (!String.IsNullOrEmpty(Source) && !Updates.IsPresent);
            }
        }

        /// <summary>
        /// Determines if a remote repository will be used to process this command.
        /// </summary>
        private bool UseRemoteSource
        {
            get
            {
                return ListAvailable.IsPresent || Updates.IsPresent || !String.IsNullOrEmpty(Source);
            }
        }

        protected virtual bool CollapseVersions
        {
            get
            {
                return !AllVersions.IsPresent && ListAvailable;
            }
        }

        private IProjectManager GetProjectManager(string projectName)
        {
            Project project = SolutionManager.GetProject(projectName);
            if (project == null)
            {
                ErrorHandler.ThrowNoCompatibleProjectsTerminatingError();
            }
            IProjectManager projectManager = PackageManager.GetProjectManager(project);
            Debug.Assert(projectManager != null);

            return projectManager;
        }

        protected override void ProcessRecordCore()
        {
            if (!UseRemoteSourceOnly && !SolutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            IPackageRepository repository;
            if (UseRemoteSource)
            {
                repository = GetRemoteRepository();
            }
            else if (!String.IsNullOrEmpty(ProjectName))
            {
                // use project repository when ProjectName is specified
                repository = GetProjectManager(ProjectName).LocalRepository;
            }
            else
            {
                repository = PackageManager.LocalRepository;
            }

            IQueryable<IPackage> packages = Updates.IsPresent
                                                ? GetPackagesForUpdate(repository)
                                                : GetPackages(repository);

            // Apply VersionCollapsing, Skip and Take, in that order.
            var packagesToDisplay = FilterPackages(repository, packages);

            WritePackages(packagesToDisplay);
        }

        protected virtual IEnumerable<IPackage> FilterPackages(IPackageRepository sourceRepository, IQueryable<IPackage> packages)
        {
            if (CollapseVersions)
            {
                // In the event the client is going up against a v1 feed, do not try to fetch pre release packages since this flag does not exist.
                if (IncludePrerelease && sourceRepository.SupportsPrereleasePackages)
                {
                    // Review: We should change this to show both the absolute latest and the latest versions but that requires changes to our collapsing behavior.
                    packages = packages.Where(p => p.IsAbsoluteLatestVersion);
                }
                else
                {
                    packages = packages.Where(p => p.IsLatestVersion);
                }
            }

            if (UseRemoteSourceOnly && _firstValueSpecified)
            {
                // Optimization: If First parameter is specified, we'll wrap the IQueryable in a BufferedEnumerable to prevent consuming the entire result set.
                packages = packages.AsBufferedEnumerable(First * 3).AsQueryable();
            }

            IEnumerable<IPackage> packagesToDisplay = packages.AsEnumerable()
                                                              .Where(PackageExtensions.IsListed);

            // When querying a remote source, collapse versions unless AllVersions is specified.
            // We need to do this as the last step of the Queryable as the filtering occurs on the client.
            if (CollapseVersions)
            {
                // Review: We should perform the Listed check over OData for better perf
                packagesToDisplay = packagesToDisplay.AsCollapsed();
            }

            if (ListAvailable && !IncludePrerelease)
            {
                // If we aren't collapsing versions, and the pre-release flag is not set, only display release versions when displaying from a remote source.
                // We don't need to filter packages when showing installed packages.
                packagesToDisplay = packagesToDisplay.Where(p => p.IsReleaseVersion());
            }

            packagesToDisplay = packagesToDisplay.Skip(Skip);

            if (_firstValueSpecified)
            {
                packagesToDisplay = packagesToDisplay.Take(First);
            }

            return packagesToDisplay;
        }

        /// <summary>
        /// Determines the remote repository to be used based on the state of the solution and the Source parameter
        /// </summary>
        private IPackageRepository GetRemoteRepository()
        {
            IPackageRepository repository;

            if (!String.IsNullOrEmpty(Source))
            {
                _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source);
                // If a Source parameter is explicitly specified, use it
                repository = CreateRepositoryFromSource(_repositoryFactory, _packageSourceProvider, Source);
            }
            else if (SolutionManager.IsSolutionOpen)
            {
                _hasConnectedToHttpSource |= UriHelper.IsHttpSource(_packageSourceProvider);
                // If the solution is open, retrieve the cached repository instance
                repository = PackageManager.SourceRepository;
            }
            else if (_packageSourceProvider.ActivePackageSource != null)
            {
                _hasConnectedToHttpSource |= UriHelper.IsHttpSource(_packageSourceProvider);
                // No solution available. Use the repository Url to create a new repository
                repository = _repositoryFactory.CreateRepository(_packageSourceProvider.ActivePackageSource.Source);
            }
            else
            {
                // No active source has been specified. 
                throw new InvalidOperationException(Resources.Cmdlet_NoActivePackageSource);
            }

            if (IsRepositoryUsedForSearch(repository))
            {
                repository = new AggregateRepository(new[] { repository });
            }

            return repository;
        }

        private bool IsRepositoryUsedForSearch(IPackageRepository repository)
        {
            // Bug #2761: The Search() service method on nuget.org doesn't handle $skiptoken, resulting in incorrect results.
            // As a work around, we wrap the repository in a AggregateRepository, which use $top and $skip options in the query.
            return !Updates.IsPresent &&
                            !String.IsNullOrEmpty(Filter) &&
                            repository is IServiceBasedRepository &&
                            !(repository is AggregateRepository);
        }

        protected virtual IQueryable<IPackage> GetPackages(IPackageRepository sourceRepository)
        {
            bool effectiveIncludePrerelease = IncludePrerelease || !UseRemoteSource;

            IQueryable<IPackage> packages = String.IsNullOrEmpty(Filter)
                                                ? sourceRepository.GetPackages()
                                                : sourceRepository.Search(Filter, effectiveIncludePrerelease);
            // by default, sort packages by Id
            packages = packages.OrderBy(p => p.Id);
            return packages;
        }

        protected virtual IQueryable<IPackage> GetPackagesForUpdate(IPackageRepository sourceRepository)
        {
            IPackageRepository localRepository = PackageManager.LocalRepository;
            var packagesToUpdate = localRepository.GetPackages();

            if (!String.IsNullOrEmpty(Filter))
            {
                packagesToUpdate = packagesToUpdate.Find(Filter);
            }

            return sourceRepository.GetUpdates(packagesToUpdate, IncludePrerelease, AllVersions).AsQueryable();
        }

        private void WritePackages(IEnumerable<IPackage> packages)
        {
            bool hasPackage = false;
            foreach (var package in packages)
            {
                // exit early if ctrl+c pressed
                if (Stopping)
                {
                    break;
                }
                hasPackage = true;

                var pso = new PSObject(package);
                if (!pso.Properties.Any(p => p.Name == "IsUpdate"))
                {
                    pso.Properties.Add(new PSNoteProperty("IsUpdate", Updates.IsPresent));
                }

                WriteObject(pso);
            }

            if (!hasPackage)
            {
                if (!UseRemoteSource)
                {
                    Logger.Log(MessageLevel.Info, Resources.Cmdlet_NoPackagesInstalled);
                }
                else if (Updates.IsPresent)
                {
                    Logger.Log(MessageLevel.Info, Resources.Cmdlet_NoPackageUpdates);
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
    }
}
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.PowerShell", "PS1101:AllCmdletsShouldAcceptPipelineInput", Justification = "Will investiage this one.")]
    [Cmdlet(VerbsCommon.Get, "Package", DefaultParameterSetName = ParameterAttribute.AllParameterSets)]
    [OutputType(typeof(IPackage))]
    public class GetPackageCommand : NuGetBaseCommand
    {
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepository _recentPackagesRepository;
        private readonly IProductUpdateService _productUpdateService;
        private int _firstValue;
        private bool _firstValueSpecified;
        private bool _hasConnectedToHttpSource;

        public GetPackageCommand()
            : this(ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IRecentPackageRepository>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>(),
                   ServiceLocator.GetInstance<IProductUpdateService>())
        {
        }

        public GetPackageCommand(IPackageRepositoryFactory repositoryFactory,
                                IVsPackageSourceProvider packageSourceProvider,
                                ISolutionManager solutionManager,
                                IVsPackageManagerFactory packageManagerFactory,
                                IPackageRepository recentPackagesRepository,
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
            if (recentPackagesRepository == null)
            {
                throw new ArgumentNullException("recentPackagesRepository");
            }

            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            _recentPackagesRepository = recentPackagesRepository;
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

        [Parameter(Mandatory = true, ParameterSetName = "Recent")]
        public SwitchParameter Recent { get; set; }

        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "Recent")]
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
                return ListAvailable.IsPresent || (!String.IsNullOrEmpty(Source) && !Updates.IsPresent) || Recent.IsPresent;
            }
        }

        /// <summary>
        /// Determines if a remote repository will be used to process this command.
        /// </summary>
        private bool UseRemoteSource
        {
            get
            {
                return ListAvailable.IsPresent || Updates.IsPresent || !String.IsNullOrEmpty(Source) || Recent.IsPresent;
            }
        }

        protected virtual bool CollapseVersions
        {
            get
            {
                return !AllVersions.IsPresent && (ListAvailable || Recent);
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

            IQueryable<IPackage> packages;
            if (Updates.IsPresent)
            {
                packages = GetPackagesForUpdate(repository);
            }
            else
            {
                packages = GetPackages(repository);
            }

            // Apply VersionCollapsing, Skip and Take, in that order.
            var packagesToDisplay = FilterPackages(repository, packages);

            WritePackages(packagesToDisplay);
        }

        protected virtual IEnumerable<IPackage> FilterPackages(IPackageRepository sourceRepository, IQueryable<IPackage> packages)
        {
            if (CollapseVersions)
            {
                // In the event the client is going up against a v1 feed, do not try to fetch pre release packages since this flag does not exist.
                if (Recent || (IncludePrerelease && sourceRepository.SupportsPrereleasePackages))
                {
                    // For Recent packages, we want to show the highest package even if it is a recent. 
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

            IEnumerable<IPackage> packagesToDisplay = packages;

            // When querying a remote source, collapse versions unless AllVersions is specified.
            // We need to do this as the last step of the Queryable as the filtering occurs on the client.
            if (CollapseVersions)
            {
                // Review: We should perform the Listed check over OData for better perf
                packagesToDisplay = packages.AsEnumerable()
                                            .Where(p => p.Listed || p.Published > NuGetConstants.Unpublished)
                                            .AsCollapsed();
            }

            if (ListAvailable && !IncludePrerelease)
            {
                // If we aren't collapsing versions, and the pre-release flag is not set, only display release versions when displaying from a remote source.
                // We don't need to filter packages when showing recent packages or installed packages.
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
            if (!String.IsNullOrEmpty(Source))
            {
                _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source);
                // If a Source parameter is explicitly specified, use it
                return CreateRepositoryFromSource(_repositoryFactory, _packageSourceProvider, Source);
            }
            else if (Recent.IsPresent)
            {
                return _recentPackagesRepository;
            }
            else if (SolutionManager.IsSolutionOpen)
            {
                _hasConnectedToHttpSource |= IsHttpSource(_packageSourceProvider);
                // If the solution is open, retrieve the cached repository instance
                return PackageManager.SourceRepository;
            }
            else if (_packageSourceProvider.ActivePackageSource != null)
            {
                _hasConnectedToHttpSource |= IsHttpSource(_packageSourceProvider);
                // No solution available. Use the repository Url to create a new repository
                return _repositoryFactory.CreateRepository(_packageSourceProvider.ActivePackageSource.Source);
            }
            else
            {
                // No active source has been specified. 
                throw new InvalidOperationException(Resources.Cmdlet_NoActivePackageSource);
            }
        }

        private static bool IsHttpSource(IVsPackageSourceProvider packageSourceProvider)
        {
            var activeSource = packageSourceProvider.ActivePackageSource;
            if (activeSource.IsAggregate())
            {
                return packageSourceProvider.GetEnabledPackageSources().Any(s => UriHelper.IsHttpSource(s.Source));
            }
            else
            {
                return UriHelper.IsHttpSource(activeSource.Source);
            }
        }

        protected virtual IQueryable<IPackage> GetPackages(IPackageRepository sourceRepository)
        {
            IQueryable<IPackage> packages = null;

            if (String.IsNullOrEmpty(Filter))
            {
                packages = sourceRepository.GetPackages();
            }
            else
            {
                packages = sourceRepository.Search(Filter);
            }

            // for recent packages, we want to order by last installed first instead of Id
            if (!Recent.IsPresent)
            {
                packages = packages.OrderBy(p => p.Id);
            }

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

            return sourceRepository.GetUpdates(packagesToUpdate, IncludePrerelease).AsQueryable();
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
                    Log(MessageLevel.Info, Resources.Cmdlet_NoPackagesInstalled);
                }
                else if (Updates.IsPresent)
                {
                    Log(MessageLevel.Info, Resources.Cmdlet_NoPackageUpdates);
                }
                else if (Recent.IsPresent)
                {
                    Log(MessageLevel.Info, Resources.Cmdlet_NoRecentPackages);
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
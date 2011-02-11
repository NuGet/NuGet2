using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands {

    /// <summary>
    /// This command lists the available packages which are either from a package source or installed in the current solution.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.PowerShell", "PS1101:AllCmdletsShouldAcceptPipelineInput", Justification = "Will investiage this one.")]
    [Cmdlet(VerbsCommon.Get, "Package", DefaultParameterSetName = ParameterAttribute.AllParameterSets)]
    [OutputType(typeof(IPackage))]
    public class GetPackageCommand : NuGetBaseCommand {
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepository _recentPackagesRepository;
        private int _firstValue;
        private bool _firstValueSpecified;

        public GetPackageCommand()
            : this(ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IPackageSourceProvider>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IRecentPackageRepository>()) {
        }

        public GetPackageCommand(IPackageRepositoryFactory repositoryFactory,
                                IPackageSourceProvider packageSourceProvider,
                                ISolutionManager solutionManager,
                                IVsPackageManagerFactory packageManagerFactory,
                                IPackageRepository recentPackagesRepository)
            : base(solutionManager, packageManagerFactory) {

            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }
            if (recentPackagesRepository == null) {
                throw new ArgumentNullException("recentPackagesRepository");
            }

            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            _recentPackagesRepository = recentPackagesRepository;
        }

        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Remote")]
        [Alias("Online", "Remote")]
        public SwitchParameter ListAvailable { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Updates")]
        public SwitchParameter Updates { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Recent")]
        public SwitchParameter Recent { get; set; }

        [Parameter(Position = 1, ParameterSetName = "Remote")]
        [Parameter(Position = 1, ParameterSetName = "Updates")]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public int First {
            get {
                return _firstValue;
            }
            set {
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
        private bool UseRemoteSourceOnly {
            get {
                return ListAvailable.IsPresent || (!String.IsNullOrEmpty(Source) && !Updates.IsPresent) || Recent.IsPresent;
            }
        }

        /// <summary>
        /// Determines if a remote repository will be used to process this command.
        /// </summary>
        private bool UseRemoteSource {
            get {
                return ListAvailable.IsPresent || Updates.IsPresent || !String.IsNullOrEmpty(Source) || Recent.IsPresent;
            }
        }

        protected override void ProcessRecordCore() {
            if (!UseRemoteSourceOnly && !SolutionManager.IsSolutionOpen) {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            IPackageRepository repository;
            if (UseRemoteSource) {
                repository = GetRemoteRepository();
            }
            else {
                repository = PackageManager.LocalRepository;
            }

            IEnumerable<IPackage> packages;
            if (Updates.IsPresent) {
                packages = FilterPackagesForUpdate(repository);
            }
            else {
                packages = FilterPackages(repository);
            }

            var filteredPackages = packages.AsQueryable().Skip(Skip);

            if (_firstValueSpecified) {
                filteredPackages = filteredPackages.Take(First);
            }

            WritePackages(filteredPackages);
        }

        /// <summary>
        /// Determines the remote repository to be used based on the state of the solution and the Source parameter
        /// </summary>
        private IPackageRepository GetRemoteRepository() {
            if (!String.IsNullOrEmpty(Source)) {
                // If a Source parameter is explicitly specified, use it
                return _repositoryFactory.CreateRepository(Source);
            }
            else if (Recent.IsPresent) {
                return _recentPackagesRepository;
            }
            else if (SolutionManager.IsSolutionOpen) {
                // If the solution is open, retrieve the cached repository instance
                return PackageManager.SourceRepository;
            }
            else if (_packageSourceProvider.ActivePackageSource != null) {
                // No solution available. Use the repository Url to create a new repository
                return _repositoryFactory.CreateRepository(_packageSourceProvider.ActivePackageSource);
            }
            else {
                // No active source has been specified. 
                throw new InvalidOperationException(Resources.Cmdlet_NoActivePackageSource);
            }
        }

        protected virtual IEnumerable<IPackage> FilterPackages(IPackageRepository sourceRepository) {
            var packages = sourceRepository.GetPackages();
            if (!String.IsNullOrEmpty(Filter)) {
                packages = packages.Find(Filter.Split());
            }
            return packages.OrderBy(p => p.Id);
        }

        protected virtual IEnumerable<IPackage> FilterPackagesForUpdate(IPackageRepository sourceRepository) {
            IPackageRepository localRepository = PackageManager.LocalRepository;
            var packagesToUpdate = localRepository.GetPackages();
            if (!String.IsNullOrEmpty(Filter)) {
                packagesToUpdate = packagesToUpdate.Find(Filter.Split());
            }
            return sourceRepository.GetUpdates(packagesToUpdate);
        }

        private void WritePackages(IQueryable<IPackage> packages) {
            
            int total;
            int packagesSoFar = 0;
            bool showProgress = ShouldShowProgress(packages, out total);

            bool hasPackage = false;
            foreach (var package in packages) {
                // exit early if ctrl+c pressed
                if (Stopping) {
                    break;
                }
                hasPackage = true;
                WriteObject(package);

                if (showProgress) {
                    packagesSoFar++;

                    // only update progress after every 20 packages 
                    if (packagesSoFar % 20 == 0) {
                        WriteProgress(
                            ProgressActivityIds.GetPackageId, Resources.Cmdlet_GetPackageProgress, (packagesSoFar * 100 / total));
                    }
                }
            }

            if (!hasPackage) {
                if (!UseRemoteSource) {
                    Log(MessageLevel.Info, Resources.Cmdlet_NoPackagesInstalled);
                }
                else if (Updates.IsPresent) {
                    Log(MessageLevel.Info, Resources.Cmdlet_NoPackageUpdates);
                }
                else if (Recent.IsPresent) {
                    Log(MessageLevel.Info, Resources.Cmdlet_NoRecentPackages);
                }
            }
        }

        private bool ShouldShowProgress(IQueryable<IPackage> packages, out int total) {
            const int ThresholdToShowProgress = 20;

            bool showProgress = UseRemoteSource;

            total = 0;
            if (showProgress) {
                total = int.MaxValue;

                if (_firstValueSpecified) {
                    // work around issue with AggregateQuery not reporting accurate result in this case
                    total = Math.Min(total, First);
                }

                // if there are too few packages, don't bother to show progress.
                if (total < ThresholdToShowProgress) {
                    showProgress = false;
                }
                else {
                    // calling packages.Count() will potentially make a web request,
                    // so we try to avoid it if possible
                    total = Math.Min(total, packages.Count());

                    showProgress = total >= ThresholdToShowProgress;
                }
            }

            return showProgress;
        }
    }
}
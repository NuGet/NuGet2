using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio.Cmdlets {

    /// <summary>
    /// This command lists the available packages which are either from a package source or installed in the current solution.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Package", DefaultParameterSetName = "Default")]
    public class GetPackageCmdlet : NuGetBaseCmdlet {
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _packageSourceProvider;

        public GetPackageCmdlet()
            : this(ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IPackageSourceProvider>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>()) {
        }

        public GetPackageCmdlet(IPackageRepositoryFactory repositoryFactory,
                                IPackageSourceProvider packageSourceProvider,
                                ISolutionManager solutionManager,
                                IVsPackageManagerFactory packageManagerFactory)
            : base(solutionManager, packageManagerFactory) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        [Parameter(Position = 0)]
        public string Filter { get; set; }

        [Parameter(Position = 1, ParameterSetName = "Remote")]
        [Alias("Online")]
        public SwitchParameter Remote { get; set; }

        [Parameter(Position = 1, ParameterSetName = "Updates")]
        public SwitchParameter Updates { get; set; }

        [Parameter(Position = 2, ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "Updates")]
        public string Source { get; set; }

        protected override void ProcessRecordCore() {
            if (!SolutionManager.IsSolutionOpen && (!Remote.IsPresent || Updates.IsPresent)) {
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            IPackageRepository repository;
            if (Remote.IsPresent || Updates.IsPresent) {
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
            WritePackages(packages);
        }

        /// <summary>
        /// Determines the remote repository to be used based on the state of the solution and the Source parameter
        /// </summary>
        private IPackageRepository GetRemoteRepository() {
            if (!String.IsNullOrEmpty(Source)) {
                // If a Source parameter is explicitly specified, use it
                return _repositoryFactory.CreateRepository(new PackageSource(Source, Source));
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
                throw new InvalidOperationException(VsResources.NoActivePackageSource);
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
            return localRepository.GetUpdates(sourceRepository, packagesToUpdate);
        }

        private void WritePackages(IEnumerable<IPackage> packages) {
            var query = from p in packages
                        select new {
                            Id = p.Id,
                            Version = p.Version,
                            Description = p.Description
                        };

            WriteObject(query, enumerateCollection: true);
        }
    }
}

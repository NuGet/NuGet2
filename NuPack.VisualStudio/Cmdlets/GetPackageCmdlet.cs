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
            : this(CachedRepositoryFactory.Instance, 
                   VsPackageSourceProvider.GetSourceProvider(DTEExtensions.DTE), 
                   NuGet.VisualStudio.SolutionManager.Current,
                   DefaultVsPackageManagerFactory.Instance) {
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

        [Parameter(Position = 1, ParameterSetName = "Installed")]
        public SwitchParameter Installed { get; set; }

        [Parameter(Position = 2, ParameterSetName = "Updates")]
        public SwitchParameter Updates { get; set; }

        [Parameter(Position = 3, ParameterSetName = "Default")]
        [Parameter(ParameterSetName = "Updates")]
        public string Source { get; set; }

        private string ActivePackageSource {
            get {
                if (_packageSourceProvider.ActivePackageSource != null) {
                    return _packageSourceProvider.ActivePackageSource.Source;
                }
                return null;
            }
        }
        protected override void ProcessRecordCore() {
            if (!SolutionManager.IsSolutionOpen && (Installed.IsPresent || Updates.IsPresent)) {
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            IPackageRepository repository;
            if (Installed.IsPresent) {
                repository = PackageManager.LocalRepository;
            }
            else if (!String.IsNullOrEmpty(Source)) {
                repository = _repositoryFactory.CreateRepository(Source);
            }
            else if (SolutionManager.IsSolutionOpen) {
                repository = PackageManager.SourceRepository;
            }
            else if (!String.IsNullOrEmpty(ActivePackageSource)) {
                repository = _repositoryFactory.CreateRepository(ActivePackageSource);
            }
            else {
                throw new InvalidOperationException(VsResources.NoActivePackageSource);
            }

            if (Updates.IsPresent) {
                ShowUpdatePackages(repository, Filter);
            }
            else {
                WritePackagesFromRepository(repository, Filter);
            }
        }

        private void WritePackagesFromRepository(IPackageRepository repository, string filter) {
            if (!String.IsNullOrEmpty(filter)) {
                WritePackages(repository.GetPackages(filter.Split()));
            }
            else {
                WritePackages(repository.GetPackages());
            }
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

        private void ShowUpdatePackages(IPackageRepository repository, string filter) {
            IEnumerable<IPackage> updates = PackageManager.LocalRepository.GetUpdates(repository, filter);
            WritePackages(updates);
        }
    }
}

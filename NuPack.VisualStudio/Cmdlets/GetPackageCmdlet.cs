using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This command lists the available packages which are either from a package source or installed in the current solution.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Package")]
    public class GetPackageCmdlet : NuPackBaseCmdlet {
        private readonly IPackageRepositoryFactory _repositoryFactory;

        public GetPackageCmdlet()
            : this(PackageRepositoryFactory.Default) {
        }

        public GetPackageCmdlet(IPackageRepositoryFactory repositoryFactory) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            _repositoryFactory = repositoryFactory;
        }

        [Parameter(Position = 0)]
        public string Filter { get; set; }

        [Parameter(Position = 1)]
        public SwitchParameter Installed { get; set; }

        [Parameter(Position = 2)]
        public SwitchParameter Updates { get; set; }

        [Parameter(Position = 3)]
        public string Source { get; set; }

        protected override void ProcessRecordCore() {
            if (!IsSolutionOpen && (Installed.IsPresent || Updates.IsPresent)) {
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            if (Updates.IsPresent) {
                ShowUpdatePackages(Filter);
            }
            IPackageRepository repository;
            if (!String.IsNullOrEmpty(Source)) {
                repository = _repositoryFactory.CreateRepository(Source);
            }
            else if (Installed.IsPresent) {
                repository = PackageManager.LocalRepository;
            }
            else if (IsSolutionOpen) {
                repository = PackageManager.SourceRepository;
            }
            else {
                repository = _repositoryFactory.CreateRepository(ActivePackageSource);
            }
            WritePackagesFromRepository(repository, Filter);
        }

        private void WritePackagesFromRepository(IPackageRepository repository, string filter) {
            WritePackages(repository.GetPackages(filter));
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

        private void ShowUpdatePackages(string filter) {
            IEnumerable<IPackage> updates = PackageManager.LocalRepository.GetUpdates(PackageManager.SourceRepository, filter);
            WritePackages(updates);
        }

        private static string ActivePackageSource {
            get {
                var packageSourceProvider = VSPackageSourceProvider.GetSourceProvider(DTEExtensions.DTE);

                if (packageSourceProvider != null && packageSourceProvider.ActivePackageSource != null) {
                    return packageSourceProvider.ActivePackageSource.Source;
                }
                else {
                    return null;
                }
            }
        }
    }
}
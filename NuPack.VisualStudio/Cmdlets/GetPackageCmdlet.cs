using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This command lists the available packages which are either from a package source or installed in the current solution.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Package", DefaultParameterSetName = "Default")]
    public class GetPackageCmdlet : NuPackBaseCmdlet {
        private readonly IPackageRepositoryFactory _repositoryFactory;

        public GetPackageCmdlet()
            : this(PackageRepositoryFactory.Default) {
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


        public GetPackageCmdlet(IPackageRepositoryFactory repositoryFactory) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            _repositoryFactory = repositoryFactory;
        }


        protected override void ProcessRecordCore() {
            if (!IsSolutionOpen && (Installed.IsPresent || Updates.IsPresent)) {
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
            else if (IsSolutionOpen) {
                repository = PackageManager.SourceRepository;
            }
            else {
                repository = _repositoryFactory.CreateRepository(ActivePackageSource);
            }

            if (Updates.IsPresent) {
                ShowUpdatePackages(repository, Filter);
            }
            else {
                WritePackagesFromRepository(repository, Filter);
            }
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

        private void ShowUpdatePackages(IPackageRepository repository, string filter) {
            IEnumerable<IPackage> updates = PackageManager.LocalRepository.GetUpdates(repository, filter);
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
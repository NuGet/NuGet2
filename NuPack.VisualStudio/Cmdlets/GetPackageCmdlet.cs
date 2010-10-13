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

        [Parameter(Position=0)]
        public string Filter { get; set; }

        [Parameter]
        public SwitchParameter Installed { get; set; }

        [Parameter]
        public SwitchParameter Updates { get; set; }

        protected override void ProcessRecordCore() {
            if (IsSolutionOpen) {
                if (Updates.IsPresent) {
                    ShowUpdatePackages(Filter);
                }
                else if (Installed.IsPresent) {
                    WritePackagesFromRepository(PackageManager.LocalRepository, Filter);
                }
                else {
                    WritePackagesFromRepository(PackageManager.SourceRepository, Filter);
                }
            }
            else {
                if (Installed.IsPresent || Updates.IsPresent) {
                    WriteError(VsResources.Cmdlet_NoSolution);
                    return;
                }

                var packageSource = ActivePackageSource;
                if (!String.IsNullOrEmpty(packageSource)) {
                    WritePackagesFromRepository(_repositoryFactory.CreateRepository(packageSource), Filter);
                }
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
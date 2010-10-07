using System;
using System.Linq;
using System.Management.Automation;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This command lists the available packages which are either from a package source or installed in the current solution.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Package")]
    public class GetPackageCmdlet : NuPackBaseCmdlet {

        #region Parameters

        [Parameter]
        public SwitchParameter Installed { get; set; }

        [Parameter]
        public SwitchParameter Updates { get; set; }

        #endregion

        protected override void ProcessRecordCore() {
            if (IsSolutionOpen) {
                if (Updates.IsPresent) {
                    ShowUpdatePackages();
                }
                else if (Installed.IsPresent) {
                    GetPackagesFromRepository(PackageManager.LocalRepository);
                }
                else {
                    GetPackagesFromRepository(PackageManager.SourceRepository);
                }
            }
            else {
                if (Installed.IsPresent || Updates.IsPresent) {
                    WriteError(VsResources.Cmdlet_NoSolution);
                    return;
                }

                var packageSource = ActivePackageSource;
                if (!String.IsNullOrEmpty(packageSource)) {
                    GetPackagesFromRepository(PackageRepositoryFactory.CreateRepository(packageSource));
                }
            }
        }

        private void GetPackagesFromRepository(IPackageRepository repository) {
            var q = from p in repository.GetPackages().AsEnumerable()
                    select new { Id = p.Id, Version = p.Version, Description = p.Description };

            WriteObject(q, enumerateCollection: true);
        }

        private void ShowUpdatePackages() {
            var solutionPackages = PackageManager.LocalRepository.GetPackages().AsEnumerable();
            var externalPackages = PackageManager.SourceRepository.GetPackages().AsEnumerable();

            // inner join
            var q = from s in solutionPackages
                    join e in externalPackages on true equals true
                    where s.Id.Equals(e.Id, StringComparison.OrdinalIgnoreCase) && s.Version < e.Version
                    select new {
                        Id = e.Id,
                        CurrentVersion = s.Version,
                        NewVersion = e.Version
                    };

            WriteObject(q, enumerateCollection: true);
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
using System;
using System.Linq;
using System.Management.Automation;
using EnvDTE;

namespace NuPack.VisualStudio.Cmdlets {

    [Cmdlet(VerbsCommon.Get, "Package")]
    public class GetPackageCmdlet : NuPackBaseCmdlet {

        #region Parameters
        
        [Parameter]
        public SwitchParameter Installed { get; set; }

        [Parameter]
        public SwitchParameter Updates { get; set; }

        #endregion

        protected override void ProcessRecordCore() {
            base.ProcessRecord();

            IPackageRepository repository = null;

            DTE dte = DTEExtensions.DTE;
            if (dte != null && dte.Solution != null && dte.Solution.IsOpen) {
                if (Updates.IsPresent) {
                    ShowUpdatePackages();
                    return;
                }
                else if (Installed.IsPresent) {
                    repository = PackageManager.LocalRepository;
                }
                else {
                    repository = PackageManager.SourceRepository;
                }
            }
            else {
                if (Installed.IsPresent || Updates.IsPresent) {
                    WriteError("The current environment doesn't have a solution open.", "Get-Package");
                    return;
                }

                var packageSource = ActivePackageSource;
                if (!String.IsNullOrEmpty(packageSource)) {
                    repository = PackageRepositoryFactory.CreateRepository(packageSource);
                }
                else {
                    return;
                }
            }

            if (repository != null) {
                var q = from p in repository.GetPackages()
                        select new { Id = p.Id, Version = p.Version, Description = p.Description };
                if (q.Any()) {
                    WriteObject(q);
                }
                else {
                    WriteLine("There is no package found.");
                }
            }
        }

        private void ShowUpdatePackages() {
            var solutionPackages = PackageManager.LocalRepository.GetPackages();
            var externalPackages = PackageManager.SourceRepository.GetPackages();

            var q = from s in solutionPackages
                    join e in externalPackages on true equals true
                    where s.Id.Equals(e.Id, StringComparison.OrdinalIgnoreCase) && s.Version < e.Version
                    select new {
                        Id = e.Id,
                        CurrentVersion = s.Version,
                        NewVersion = e.Version
                    };

            if (q.Any()) {
                WriteObject(q);
            }
            else {
                WriteLine("There is no updates found.");
            }
        }

        private string ActivePackageSource {
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
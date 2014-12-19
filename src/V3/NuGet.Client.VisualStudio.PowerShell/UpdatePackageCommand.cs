using Microsoft.VisualStudio.Shell;
using NuGet.Client.Resolution;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;


#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
#endif

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    /// TODO:
    /// 1. Follow up with UpdateAll UI implementation (not done yet)
    /// 2. Figure out the Utility to get list of packages installed to a project - CoreInteropInstalledPackagesList?
    /// 3. Figure out the right API to use to get latest version, safe version etc. 
    [Cmdlet(VerbsData.Update, "Package2", DefaultParameterSetName = "All")]
    public class UpdatePackageCommand : PackageInstallBaseCommand
    {
        private IProductUpdateService _productUpdateService;
        private bool _hasConnectedToHttpSource;
        private bool _idSpecified;
        private bool _projectSpecified;

        public UpdatePackageCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>())
        {
            _productUpdateService = ServiceLocator.GetInstance<IProductUpdateService>();
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Project")]
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "All")]
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Reinstall")]
        public override string Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
                _idSpecified = true;
            }
        }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "All")]
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "Project")]
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "Reinstall")]
        public override string ProjectName
        {
            get
            {
                return base.ProjectName;
            }
            set
            {
                base.ProjectName = value;
                _projectSpecified = true;
            }
        }

        [Parameter(Position = 2, ParameterSetName = "Project")]
        [ValidateNotNullOrEmpty]
        public override string Version { get; set; }

        [Parameter]
        public SwitchParameter Safe { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Reinstall")]
        [Parameter(ParameterSetName = "All")]
        public SwitchParameter Reinstall { get; set; }

        protected override void Preprocess()
        {
            base.Preprocess();
            if (!_projectSpecified)
            {
                this.Projects = GetAllProjectsInSolution();
            }
        }

        protected override void ExecutePackageActions()
        {
            SubscribeToProgressEvents();

            if (!Reinstall.IsPresent)
            {
                PerformPackageUpdates();
            }
            else
            {
                PerformPackageReinstalls();
            }

            UnsubscribeFromProgressEvents();
        }

        private void PerformPackageUpdates()
        {
            // UpdateAll
            if (!_idSpecified)
            {
                Dictionary<VsProject, List<PackageIdentity>> dictionary = GetInstalledPackagesForAllProjects();
                foreach (KeyValuePair<VsProject, List<PackageIdentity>> entry in dictionary)
                {
                    IEnumerable<VsProject> targetedProjects = new List<VsProject> { entry.Key };
                    List<PackageIdentity> identities = entry.Value;
                    // Execute update for each of the project inside the solution
                    foreach (PackageIdentity identity in identities)
                    {
                        // Find packages update
                        PackageIdentity update = PowerShellPackage.GetLastestUpdateForPackage(ActiveSourceRepository, identity, entry.Key, IncludePrerelease.IsPresent, Safe.IsPresent);
                        ExecuteSinglePackageAction(update, targetedProjects);
                    }
                }
            }
            else
            {
                Dictionary<VsProject, PackageIdentity> dictionary = GetInstalledPackageWithId(Id);
                foreach (KeyValuePair<VsProject, PackageIdentity> entry in dictionary)
                {
                    IEnumerable<VsProject> targetedProjects = new List<VsProject> { entry.Key };
                    PackageIdentity identity = entry.Value;
                    PackageIdentity update = null;
                    // Find package update
                    if (!string.IsNullOrEmpty(Version))
                    {
                        NuGetVersion nVersion = PowerShellPackage.GetNuGetVersionFromString(Version);
                        PackageIdentity pIdentity = new PackageIdentity(Id, nVersion);
                        update = Client.PackageRepositoryHelper.ResolvePackage(ActiveSourceRepository, V2LocalRepository, pIdentity, IncludePrerelease.IsPresent);
                    }
                    else
                    {
                        update = PowerShellPackage.GetLastestUpdateForPackage(ActiveSourceRepository, identity, entry.Key, IncludePrerelease.IsPresent, Safe.IsPresent);
                    }

                    ExecuteSinglePackageAction(update, targetedProjects);
                }
            }
        }

        private void PerformPackageReinstalls()
        {
            // ReinstallAll
            if (!_idSpecified)
            {
                Dictionary<VsProject, List<PackageIdentity>> dictionary = GetInstalledPackagesForAllProjects();
                foreach (KeyValuePair<VsProject, List<PackageIdentity>> entry in dictionary)
                {
                    IEnumerable<VsProject> targetedProjects = new List<VsProject> { entry.Key };
                    ForceInstallPackages(entry.Value, targetedProjects);
                }
            }
            else
            {
                Dictionary<VsProject, PackageIdentity> dictionary = GetInstalledPackageWithId(Id);
                foreach (KeyValuePair<VsProject, PackageIdentity> entry in dictionary)
                {
                    IEnumerable<VsProject> targetedProjects = new List<VsProject> { entry.Key };
                    PackageIdentity resolvedIdentity = Client.PackageRepositoryHelper.ResolvePackage(ActiveSourceRepository, V2LocalRepository, entry.Value, IncludePrerelease.IsPresent);
                    ForceInstallPackage(resolvedIdentity, targetedProjects);
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
            _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source);
            if (_productUpdateService != null && _hasConnectedToHttpSource)
            {
                _productUpdateService.CheckForAvailableUpdateAsync();
            }
        }
    }
}
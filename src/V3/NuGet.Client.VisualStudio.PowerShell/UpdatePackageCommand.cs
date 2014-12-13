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

        // We need to override id since it's mandatory in the base class. We don't
        // want it to be mandatory here.
        // Update-Package Reinstall feature is cut for V3. 
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Project")]
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "All")]
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

        [Parameter]
        public SwitchParameter Safe { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            this.PackageActionResolver = new ActionResolver(ActiveSourceRepository, ResolutionContext);
        }

        protected override void PreprocessProjectAndIdentities()
        {
            if (!_projectSpecified)
            {
                this.Projects = GetAllProjectsInSolution();
            }
            else
            {
                base.PreprocessProjectAndIdentities();
            }
        }

        protected override void ExecutePackageActions()
        {
            SubscribeToProgressEvents();

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
                        NuGetVersion nVersion = ParseUserInputForVersion(Version);
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

        /// <summary>
        /// Get Installed Package References for all projects.
        /// </summary>
        /// <returns></returns>
        private Dictionary<VsProject, List<PackageIdentity>> GetInstalledPackagesForAllProjects()
        {
            Dictionary<VsProject, List<PackageIdentity>> dic = new Dictionary<VsProject, List<PackageIdentity>>();
            foreach (VsProject proj in Projects)
            {
                List<PackageIdentity> list = GetInstalledReferences(proj).Select(r => r.Identity).ToList();
                dic.Add(proj, list);
            }
            return dic;
        }

        /// <summary>
        /// Get installed package identity for specific package Id in all projects.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        private Dictionary<VsProject, PackageIdentity> GetInstalledPackageWithId(string packageId)
        {
            Dictionary<VsProject, PackageIdentity> dic = new Dictionary<VsProject, PackageIdentity>();
            foreach (VsProject proj in Projects)
            {
                InstalledPackageReference reference = GetInstalledReference(proj, packageId);
                if (reference != null)
                {
                    PackageIdentity identity = reference.Identity;
                    dic.Add(proj, identity);
                }
            }
            return dic;
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
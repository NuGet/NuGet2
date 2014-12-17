using Microsoft.VisualStudio.Shell;
using NuGet.Client.Resolution;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This command uninstalls the specified package from the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Uninstall, "Package2")]
    public class UninstallPackageCommand : PackageActionBaseCommand
    {
        private ResolutionContext _context;

        public UninstallPackageCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>(),
                 PackageActionType.Uninstall)
        {
        }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter RemoveDependencies { get; set; }

        protected override void Preprocess()
        {
            base.Preprocess();
            VsProject proj = this.Projects.FirstOrDefault();
            SourceRepository localRepo = proj.TryGetFeature<SourceRepository>();
            this.ActiveSourceRepository = localRepo;
            this.PackageActionResolver = new ActionResolver(ActiveSourceRepository, ResolutionContext);
            this.Identities = GetPackageIdentityForResolver();
        }

        /// <summary>
        /// Returns single package identity for resolver when Id is specified
        /// </summary>
        /// <returns></returns>
        private List<PackageIdentity> GetPackageIdentityForResolver()
        {
            PackageIdentity identity = null;

            // If Version is specified by commandline parameter
            if (!string.IsNullOrEmpty(Version))
            {
                NuGetVersion nVersion = GetNuGetVersionFromString(Version);
                PackageIdentity pIdentity = new PackageIdentity(Id, nVersion);
                identity = Client.PackageRepositoryHelper.ResolvePackage(V2LocalRepository, pIdentity, true);
            }
            else
            {
                VsProject target = this.GetProject(true);
                InstalledPackageReference installedPackage = GetInstalledReference(target, Id);
                if (installedPackage == null)
                {
                    Log(MessageLevel.Error, Resources.Cmdlet_PackageNotInstalled, Id);
                }
                else
                {
                    identity = installedPackage.Identity;
                    Version = identity.Version.ToNormalizedString();
                }
            }

            return new List<PackageIdentity>() { identity };
        }

        public ResolutionContext ResolutionContext
        {
            get
            {
                _context = new ResolutionContext();
                _context.RemoveDependencies = RemoveDependencies.IsPresent;
                _context.ForceRemove = Force.IsPresent;
                return _context;
            }
        }
    }
}

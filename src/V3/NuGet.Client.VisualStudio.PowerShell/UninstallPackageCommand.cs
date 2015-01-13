using Microsoft.VisualStudio.Shell;
using NuGet.Client.Resolution;
using NuGet.Versioning;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This command uninstalls the specified package from the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Uninstall, "Package")]
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
            Source = V2LocalRepository.Source;
            this.ActiveSourceRepository = GetActiveRepository(Source);
            this.PackageActionResolver = new ActionResolver(
                ActiveSourceRepository,
                CreateDependencyResolutionSource(ActiveSourceRepository),
                ResolutionContext);
            this.Identities = GetPackageIdentityForResolver();
        }

        /// <summary>
        /// Returns single package identity for resolver when Id is specified
        /// </summary>
        /// <returns></returns>
        private List<PackageIdentity> GetPackageIdentityForResolver()
        {
            VsProject target = this.GetProject(true);
            InstalledPackageReference installedPackage = GetInstalledReference(target, Id);
            // If package Id cannot be found in Installed packages.
            if (installedPackage == null)
            {
                WriteError(string.Format(Resources.Cmdlet_PackageNotInstalled, Id));
            }

            PackageIdentity identity = installedPackage.Identity;
            if (!string.IsNullOrEmpty(Version))
            {
                NuGetVersion nVersion = PowerShellPackage.GetNuGetVersionFromString(Version);
                // If specified Version does not match the installed version
                if (nVersion != identity.Version)
                {
                    WriteError(string.Format(VsResources.UnknownPackageInProject, Id + " " + Version, target.Name));
                }
            }

            // Finally resolve the identity against local repository.
            PackageIdentity resolvedIdentity = Client.PackageRepositoryHelper.ResolvePackage(V2LocalRepository, identity, true);
            return new List<PackageIdentity>() { resolvedIdentity };
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

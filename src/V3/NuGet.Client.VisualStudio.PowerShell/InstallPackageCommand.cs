using Microsoft.VisualStudio.Shell;
using NuGet.Client.Resolution;
using NuGet.VisualStudio;
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
    /// TODO List
    /// 1. Filter unlisted packages from latest version, if version is not specified by user
    /// 2. Add back fall back to cache featuree
    /// 3. Add new path/package recognition feature
    /// 4. Add back WriteDisClaimer before installing packages. Should be one of the Resolver actions.
    /// 5. Add back popping up Readme.txt feature. Should be one of the Resolver actions. 
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCommand : PackageActionBaseCommand
    {
        private ResolutionContext _context;

        public InstallPackageCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>(),
                 PackageActionType.Install)
        {
        }

        [Parameter]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter]
        public Client.FileConflictAction FileConflictAction { get; set; }

        [Parameter]
        public DependencyBehavior? DependencyVersion { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            this.PackageActionResolver = new ActionResolver(ActiveSourceRepository, ResContext);
        }

        protected override void ResolvePackageFromRepository()
        {
            if (IsVersionSpecified)
            {
                PackageIdentity pIdentity = Client.PackageRepositoryHelper.ResolvePackage(ActiveSourceRepository, V2LocalRepository, Id, Version, IncludePrerelease.IsPresent);
                this.Identity = pIdentity;
            }
        }

        public override FileConflictAction ResolveFileConflict(string message)
        {
            if (FileConflictAction == FileConflictAction.Overwrite)
            {
                return Client.FileConflictAction.Overwrite;
            }

            if (FileConflictAction == FileConflictAction.Ignore)
            {
                return Client.FileConflictAction.Ignore;
            }

            return base.ResolveFileConflict(message);
        }

        public ResolutionContext ResContext
        {
            get
            {
                _context = new ResolutionContext();
                _context.DependencyBehavior = GetDependencyBehavior();
                _context.AllowPrerelease = IncludePrerelease.IsPresent;
                // If Version is prerelease, automatically allow prerelease (i.e. append -Prerelease switch).
                if (IsVersionSpecified && PowerShellPackageViewModel.IsPrereleaseVersion(this.Version))
                {
                    _context.AllowPrerelease = true;
                }
                return _context;
            }
        }

        private DependencyBehavior GetDependencyBehavior()
        {
            if (IgnoreDependencies.IsPresent)
            {
                return DependencyBehavior.Ignore;
            }
            else if (DependencyVersion.HasValue)
            {
                return DependencyVersion.Value;
            }
            else
            {
                return DependencyBehavior.Lowest;
            }
        }
    }
}
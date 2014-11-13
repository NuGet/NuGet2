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
    /// This command consolidates the specified package into the specified project.
    /// TODO List
    /// 1. Have a spec to define the behaviors of this command in details.
    /// 2. Implement the spec
    /// </summary>
    [Cmdlet("Consolidate", "Package")]
    public class ConsolidatePackageCommand : PackageActionBaseCommand
    {
        private ResolutionContext _context;

        public ConsolidatePackageCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>(),
                 PackageActionType.Install)
        {
            this.IterateProjects = true;
            this.IsConsolidating = true;
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
                // If Version is prerelease, automatically allow prerelease (i.e. append -Prerelease switch).
                if (PowerShellPackageViewModel.IsPrereleaseVersion(this.Version))
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
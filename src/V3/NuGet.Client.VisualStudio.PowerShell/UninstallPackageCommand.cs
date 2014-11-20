using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using NuGet.Client.Resolution;
using NuGet.PowerShell.Commands;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

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
                 ServiceLocator.GetInstance<IHttpClientEvents>(),
                 PackageActionType.Uninstall)
        {
            this.PackageActionResolver = new ActionResolver(V3SourceRepository, ResContext);
        }

        [Parameter]
        public DependencyBehavior DependencyBehavior { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        protected override void ResolvePackageFromRepository()
        {
            PackageIdentity pIdentity = Client.PackageRepositoryHelper.ResolvePackage(V2LocalRepository, Id, Version, IncludePrerelease.IsPresent);
            this.Identity = pIdentity;
        }

        public ResolutionContext ResContext
        {
            get
            {
                _context = new ResolutionContext();
                _context.DependencyBehavior = DependencyBehavior;
                _context.AllowPrerelease = IncludePrerelease.IsPresent;
                return _context;
            }
        }
    }
}

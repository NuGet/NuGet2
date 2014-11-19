using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using NuGet.Client;
using NuGet.Client.Interop;
using NuGet.Client.Resolution;
using NuGet.Client.VisualStudio.PowerShell;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;


#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
#endif

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Package2", DefaultParameterSetName = "All")]
    public class UpdatePackageCommand : PackageActionBaseCommand
    {
        private ResolutionContext _context;

        public UpdatePackageCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>(),
                 PackageActionType.Install)
        {
            this.PackageActionResolver = new ActionResolver(V3SourceRepository, ResContext);
        }

        [Parameter]
        public DependencyBehavior DependencyBehavior { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        protected override void ResolvePackageFromRepository()
        {
            if (IsVersionSpecified)
            {
                Client.PackageRepositoryHelper.ResolvePackage(V3SourceRepository, V2LocalRepository, Id, Version, IncludePrerelease.IsPresent);
            }
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
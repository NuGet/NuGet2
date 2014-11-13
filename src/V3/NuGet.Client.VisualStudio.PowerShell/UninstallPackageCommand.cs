using Microsoft.VisualStudio.Shell;
using NuGet.Client.Resolution;
using NuGet.VisualStudio;
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

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            // For uninstall, use local repository for better performance
            VsProject proj = GetProject(true);
            SourceRepository localRepo = proj.TryGetFeature<SourceRepository>();
            this.ActiveSourceRepository = localRepo;
            this.PackageActionResolver = new ActionResolver(ActiveSourceRepository, ResContext);
        }

        public ResolutionContext ResContext
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

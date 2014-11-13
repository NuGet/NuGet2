using Microsoft.VisualStudio.Shell;
using NuGet.Client;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;
using NuGet.Client.Resolution;
using NuGet.Client.VisualStudio;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
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
    [Cmdlet(VerbsLifecycle.Install, "Package2")]
    public class InstallPackageCommand
    {
        private ActionResolver _actionResolver;
        private VsSourceRepositoryManager _repoManager;
        private IVsPackageSourceProvider _packageSourceProvider;
        private IPackageRepositoryFactory _repositoryFactory;
        private SVsServiceProvider _serviceProvider;
        private IVsPackageManagerFactory _packageManagerFactory;
        private ISolutionManager _solutionManager;
        private VsPackageManagerContext _VsContext;
        private PackageIdentity _identity;
        private ResolutionContext _context;
        private readonly EnvDTE._DTE _dte;

        public InstallPackageCommand()
        {
            _packageSourceProvider = ServiceLocator.GetInstance<IVsPackageSourceProvider>();
            _repositoryFactory = ServiceLocator.GetInstance<IPackageRepositoryFactory>();
            _serviceProvider = ServiceLocator.GetInstance<SVsServiceProvider>();
            _packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            _repoManager = new VsSourceRepositoryManager(_packageSourceProvider, _repositoryFactory);
            _solutionManager = new SolutionManager();
            _VsContext = new VsPackageManagerContext(_repoManager, _serviceProvider, _solutionManager, _packageManagerFactory);
            _actionResolver = new ActionResolver(_repoManager.ActiveRepository, _context);

            ExecuteCommmand();
        }

        public ResolutionContext ResolutionContext
        {
            get
            {
                _context = new ResolutionContext();
                _context.DependencyBehavior = DependencyBehavior;
                _context.AllowPrerelease = IncludePrerelease.IsPresent;
                return _context;
            }
        }

        public PackageIdentity Identity
        {
            get
            {
                _identity = new PackageIdentity(Id, NuGetVersion.Parse(Version));
                return _identity;
            }
        }

        public VsSolution Solution
        {
            get
            {
                return _VsContext.GetCurrentVsSolution();
            }
        }

        public IEnumerable<VsProject> TargetedProject
        {
            get
            {
                EnvDTE.Project project = Solution.DteSolution.GetAllProjects().FirstOrDefault(p => String.Equals(p.Name, ProjectName, StringComparison.OrdinalIgnoreCase));
                VsProject vsProject = Solution.GetProject(project);
                List<VsProject> targetedProjects = new List<VsProject> {vsProject};
                return targetedProjects;
            }
        }
      
        private void ExecuteCommmand()
        {
            // Resolve Actions
            Task<IEnumerable<NuGet.Client.Resolution.PackageAction>> actions = _actionResolver.ResolveActionsAsync(_identity, PackageActionType.Install, TargetedProject, Solution);

            // Execute Actions
            ActionExecutor executor = new ActionExecutor();
            executor.ExecuteActionsAsync(actions.Result, CancellationToken.None);
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public virtual string Id { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public virtual string ProjectName { get; set; }

        [Parameter(Position = 2)]
        [ValidateNotNull]
        public string Version { get; set; }

        [Parameter]
        public DependencyBehavior DependencyBehavior { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }
    }
}
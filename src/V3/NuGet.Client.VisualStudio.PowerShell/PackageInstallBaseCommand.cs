using Microsoft.VisualStudio.Shell;
using NuGet.Client.Resolution;
using NuGet.VisualStudio;
using System.Management.Automation;

namespace NuGet.Client.VisualStudio.PowerShell
{
    public class PackageInstallBaseCommand : PackageActionBaseCommand
    {
        private ResolutionContext _context;

        public PackageInstallBaseCommand(
            IVsPackageSourceProvider packageSourceProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            SVsServiceProvider svcServiceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ISolutionManager solutionManager,
            IHttpClientEvents clientEvents)
            : base(packageSourceProvider, packageRepositoryFactory, svcServiceProvider, packageManagerFactory, solutionManager, clientEvents, PackageActionType.Install)
        {
        }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter]
        public Client.FileConflictAction FileConflictAction { get; set; }

        [Parameter]
        public DependencyBehavior? DependencyVersion { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            this.PackageActionResolver = new ActionResolver(ActiveSourceRepository, ResolutionContext);
        }

        protected override void PreprocessProjectAndIdentities()
        {
            base.PreprocessProjectAndIdentities();
        }

        /// <summary>
        /// Resolution Context for the command
        /// </summary>
        public ResolutionContext ResolutionContext
        {
            get
            {
                _context = new ResolutionContext();
                _context.DependencyBehavior = GetDependencyBehavior();
                _context.AllowPrerelease = IncludePrerelease.IsPresent;
                // If Version is prerelease, automatically allow prerelease (i.e. append -Prerelease switch).
                if (!string.IsNullOrEmpty(Version) && PowerShellPackage.IsPrereleaseVersion(Version))
                {
                    _context.AllowPrerelease = true;
                }
                return _context;
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

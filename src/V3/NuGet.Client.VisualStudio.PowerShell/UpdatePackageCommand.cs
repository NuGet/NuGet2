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
    /// TODO:
    /// 1. Follow up with UpdateAll UI implementation (not done yet)
    /// 2. Figure out the Utility to get list of packages installed to a project - CoreInteropInstalledPackagesList?
    /// 3. Figure out the right API to use to get latest version, safe version etc. 
    [Cmdlet(VerbsData.Update, "Package2", DefaultParameterSetName = "All")]
    public class UpdatePackageCommand : PackageActionBaseCommand
    {
        private ResolutionContext _context;
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
                 ServiceLocator.GetInstance<IHttpClientEvents>(),
                 PackageActionType.Install)
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
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter]
        public SwitchParameter Safe { get; set; }

        [Parameter]
        public FileConflictAction FileConflictAction { get; set; }

        [Parameter]
        public DependencyBehavior? DependencyVersion { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            this.PackageActionResolver = new ActionResolver(ActiveSourceRepository, ResContext);
        }

        protected override void ExecutePackageAction()
        {
            // UpdateAll
            if (!_idSpecified && !_projectSpecified)
            {
                //TODO: UpdateAll logic using NuGet.Client after UI UpdateAll is implemented.
            }
            else if (_idSpecified && !_projectSpecified)
            {
                //TODO: Update Id for every project
            }
            else if (_idSpecified && _projectSpecified)
            {
                //TODO: Update Id for specified project
                base.ExecutePackageAction();
            }
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
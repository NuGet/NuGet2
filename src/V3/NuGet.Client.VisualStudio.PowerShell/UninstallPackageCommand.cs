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
    public class UninstallPackageCommand : ProcessPackageBaseCommand
    {
        private ResolutionContext _context;
        private string _version;

        public UninstallPackageCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 PackageActionType.Uninstall)
        {
            this.PackageActionResolver = new ActionResolver(this.RepoManager.ActiveRepository, ResContext);
        }

        [Parameter(Position = 2)]
        public string Version
        {
            get
            {
                if (String.IsNullOrEmpty(_version))
                {
                    try
                    {
                        Task<IEnumerable<JObject>> packages = this.RepoManager.ActiveRepository.GetPackageMetadataById(Id);
                        var r = packages.Result;
                        var allVersions = r.Select(p => NuGetVersion.Parse(p.Value<string>(Properties.Version)));
                        _version = allVersions.OrderByDescending(v => v).FirstOrDefault().ToNormalizedString();
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.HandleException(ex, false);
                    }
                }
                return _version;
            }
            set
            {
                _version = value;
            }
        }

        [Parameter]
        public DependencyBehavior DependencyBehavior { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }


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

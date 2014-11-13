using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using NuGet.Client.Installation;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This command lists the available packages which are either from a package source or installed in the current solution.
    /// </summary>
    /// TODO List
    /// 1. Confirm that -ListAvailable is cut.
    /// 2. If ListAvailable is cut, then Source and Updates switch should be cut as well.
    [Cmdlet(VerbsCommon.Get, "Package", DefaultParameterSetName = ParameterAttribute.AllParameterSets)]
    [OutputType(typeof(IPackage))]
    public class GetPackageCommand : NuGetPowerShellBaseCommand
    {
        private readonly IProductUpdateService _productUpdateService;
        private int _firstValue;
        private bool _firstValueSpecified;
        private bool _projectNameSpecified;

        public GetPackageCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>())
        {
            _productUpdateService = ServiceLocator.GetInstance<IProductUpdateService>();
        }

        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

        //[Parameter(Mandatory = true, ParameterSetName = "Updates")]
        //public SwitchParameter Updates { get; set; }

        [Parameter]
        public override string ProjectName
        {
            get
            {
                return base.ProjectName;
            }
            set
            {
                _projectNameSpecified = true;
                base.ProjectName = value;
            }
        }

        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public int First
        {
            get
            {
                return _firstValue;
            }
            set
            {
                _firstValue = value;
                _firstValueSpecified = true;
            }
        }

        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public int Skip { get; set; }

        protected override void ProcessRecordCore()
        {
            List<InstallationTarget> targets = new List<InstallationTarget>();
            List<JObject> solutionInstalledPackages = new List<JObject>();

            if (_projectNameSpecified)
            {
                // Get current project
                VsProject target = this.GetProject(true);
                targets.Add(target);
            }
            else
            {
                targets = Solution.GetAllTargetsRecursively().ToList();
            }

            foreach (InstallationTarget target in targets)
            {
                InstalledPackagesList projectlist = target.InstalledPackages;

                // Get all installed packages and metadata for project
                Task<IEnumerable<JObject>> task = projectlist.GetAllInstalledPackagesAndMetadata();
                IEnumerable<JObject> installedObjects = task.Result.ToList();

                // Add to the solution's installed packages list
                solutionInstalledPackages.AddRange(installedObjects);
            }

            IEnumerable<JObject> installedPackages = null;

            // Filter the results by string, then skip and take
            if (!string.IsNullOrEmpty(Filter))
            {
                installedPackages = solutionInstalledPackages.Where(p => p.Value<string>(Properties.PackageId).StartsWith(Filter, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                installedPackages = solutionInstalledPackages;
            }

            installedPackages = installedPackages.Skip(Skip).ToList();
            if (_firstValueSpecified)
            {
                installedPackages = installedPackages.Take(First).ToList();
            }

            // Get the PowerShellPackageView
            var view = PowerShellPackageViewModel.GetPowerShellPackageView(installedPackages);
            WriteObject(view, enumerateCollection: true);
        }
    }
}
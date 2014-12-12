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
    /// 1. Figure out the new behavior/Command that is similar to -ListAvailable
    /// 2. For parameters that are cut/modified, emit useful message for directing users to the new useage pattern.
    [Cmdlet(VerbsCommon.Get, "Package2", DefaultParameterSetName = ParameterAttribute.AllParameterSets)]
    [OutputType(typeof(IPackage))]
    public class GetPackageCommand : NuGetPowerShellBaseCommand
    {
        private readonly IProductUpdateService _productUpdateService;
        private int _firstValue;
        private bool _firstValueSpecified;

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

        [Parameter(Position = 1, ParameterSetName = "Remote")]
        [Parameter(Position = 1, ParameterSetName = "Updates")]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Project")]
        [ValidateNotNullOrEmpty]
        public string ProjectName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Remote")]
        [Alias("Online", "Remote")]
        public SwitchParameter ListAvailable { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Updates")]
        public SwitchParameter Updates { get; set; }

        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "Updates")]
        public SwitchParameter AllVersions { get; set; }

        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "Updates")]
        [Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

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
            IEnumerable<InstallationTarget> targets = PreprocessProjects();
            List<JObject> installedJObjects = GetInstalledJObjectInSolution(targets);
            IEnumerable<JObject> installedPackages = Enumerable.Empty<JObject>();

            // Filter the results by string
            if (!string.IsNullOrEmpty(Filter))
            {
                installedPackages = installedJObjects.Where(p => p.Value<string>(Properties.PackageId).StartsWith(Filter, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                installedPackages = installedJObjects;
            }

            // Skip and then take
            installedPackages = installedPackages.Skip(Skip).ToList();
            if (_firstValueSpecified)
            {
                installedPackages = installedPackages.Take(First).ToList();
            }

            // Get the PowerShellPackageView
            var view = PowerShellPackage.GetPowerShellPackageView(installedPackages);
            WriteObject(view, enumerateCollection: true);
        }

        protected IEnumerable<InstallationTarget> PreprocessProjects()
        {
            List<InstallationTarget> targets = new List<InstallationTarget>();
            if (!string.IsNullOrEmpty(ProjectName))
            {
                // Get current project
                Project project = SolutionManager.GetProject(ProjectName);
                VsProject target = Solution.GetProject(project);
                targets.Add(target);
            }
            else
            {
                targets = Solution.GetAllTargetsRecursively().ToList();
            }
            return targets;
        }

        protected List<JObject> GetInstalledJObjectInSolution(IEnumerable<InstallationTarget> targets)
        {
            List<JObject> list = new List<JObject>();
            foreach (InstallationTarget target in targets)
            {
                InstalledPackagesList projectlist = target.InstalledPackages;
                // Get all installed packages and metadata for project
                Task<IEnumerable<JObject>> task = projectlist.GetAllInstalledPackagesAndMetadata();
                IEnumerable<JObject> installedObjects = task.Result.ToList();

                // Add to the solution's installed packages list
                list.AddRange(installedObjects);
            }
            return list;
        }
    }
}
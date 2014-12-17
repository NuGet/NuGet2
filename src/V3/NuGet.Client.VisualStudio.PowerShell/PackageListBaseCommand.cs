using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using NuGet.Client.Installation;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.PowerShell
{
    public class PackageListBaseCommand : NuGetPowerShellBaseCommand
    {
        private bool _hasConnectedToHttpSource;
        private IProductUpdateService _productUpdateService;

        public PackageListBaseCommand()
            : base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<SVsServiceProvider>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>())
        {
            _productUpdateService = ServiceLocator.GetInstance<IProductUpdateService>();
        }
                
        [Parameter(Position = 2, ParameterSetName = "Remote")]
        [Parameter(Position = 2, ParameterSetName = "Updates")]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "Updates")]
        [Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public int First { get; set; }

        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public int Skip { get; set; }

        public string TargetProjectName { get; set; }

        /// <summary>
        /// Determines if local repository are not needed to process this command
        /// </summary>
        protected bool UseRemoteSourceOnly { get; set; }

        /// <summary>
        /// Determines if a remote repository will be used to process this command.
        /// </summary>
        protected bool UseRemoteSource { get; set; }

        protected virtual bool CollapseVersions { get; set; }

        protected virtual void Preprocess()
        {
            this.ActiveSourceRepository = GetActiveRepository(Source);
        }

        protected override void ProcessRecordCore()
        {
        }

        /// <summary>
        /// Filter the installed packages list based on Filter, Skip and First parameters
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<JObject> FilterInstalledPackagesResults(string filter, int skip, int take)
        {
            IEnumerable<InstallationTarget> targets = GetProjects();
            List<JObject> installedJObjects = GetInstalledJObjectInSolution(targets);
            IEnumerable<JObject> installedPackages = Enumerable.Empty<JObject>();

            // Filter the results by string
            if (!string.IsNullOrEmpty(filter))
            {
                installedPackages = installedJObjects.Where(p => p.Value<string>(Properties.PackageId).StartsWith(filter, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                installedPackages = installedJObjects;
            }

            // Skip and then take
            installedPackages = installedPackages.Skip(skip).ToList();
            if (take != 0)
            {
                installedPackages = installedPackages.Take(take).ToList();
            }
            return installedPackages;
        }

        protected IEnumerable<JObject> GetPackagesFromRemoteSource(string packageId, IEnumerable<FrameworkName> names, bool allowPrerelease, int skip, int take)
        {
            IEnumerable<JObject> packages = Enumerable.Empty<JObject>();
            packages = PowerShellPackage.GetPackageVersions(ActiveSourceRepository, packageId, names, allowPrerelease, skip, take);
            return packages;
        }

        protected IEnumerable<JObject> GetPackageUpdatesFromRemoteSource(bool allowPrerelease, int skip, int take)
        {
            List<JObject> packages = new List<JObject>();
            Dictionary<VsProject, List<PackageIdentity>> dictionary = GetInstalledPackagesForAllProjects();
            foreach (KeyValuePair<VsProject, List<PackageIdentity>> entry in dictionary)
            {
                IEnumerable<VsProject> targetedProjects = new List<VsProject> { entry.Key };
                List<PackageIdentity> identities = entry.Value;
                // Execute update for each of the project inside the solution
                foreach (PackageIdentity identity in identities)
                {
                    // Find packages update
                    JObject update = PowerShellPackage.GetLastestJObjectForPackage(ActiveSourceRepository, identity, entry.Key, allowPrerelease, false);
                    NuGetVersion version = GetNuGetVersionFromString(update.Value<string>(Properties.Version));
                    if (version > identity.Version)
                    {
                        packages.Add(update);
                    }
                }
            }
            return packages;
        }

        /// <summary>
        /// Get Installed Package References for all projects.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<VsProject, List<PackageIdentity>> GetInstalledPackagesForAllProjects()
        {
            Dictionary<VsProject, List<PackageIdentity>> dic = new Dictionary<VsProject, List<PackageIdentity>>();
            IEnumerable<VsProject> projects = GetProjects();
            foreach (VsProject proj in projects)
            {
                List<PackageIdentity> list = GetInstalledReferences(proj).Select(r => r.Identity).ToList();
                dic.Add(proj, list);
            }
            return dic;
        }

        /// <summary>
        /// Get Installed Package References for a single project
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<InstalledPackageReference> GetInstalledReferences(VsProject proj)
        {
            IEnumerable<InstalledPackageReference> refs = Enumerable.Empty<InstalledPackageReference>();
            InstalledPackagesList installedList = proj.InstalledPackages;
            if (installedList != null)
            {
                refs = installedList.GetInstalledPackages();
            }
            return refs;
        }

        /// <summary>
        /// Get all of the installed JObjects in the solution
        /// </summary>
        /// <param name="targets"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get current projects or all projects in the solution
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<VsProject> GetProjects()
        {
            List<VsProject> projects = new List<VsProject>();
            if (string.IsNullOrEmpty(TargetProjectName))
            {
                projects = GetAllProjectsInSolution().ToList();
            }
            else
            {
                VsProject project = GetProject(TargetProjectName, true);
                projects.Add(project);
            }
            return projects;
        }

        protected void WritePackages(IEnumerable<JObject> packages, VersionType versionType)
        {
            // Get the PowerShellPackageView
            var view = PowerShellPackage.GetPowerShellPackageView(packages, versionType);
            WriteObject(view, enumerateCollection: true);
        }

        protected void CheckForNuGetUpdate()
        {
            _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source);
            if (_productUpdateService != null && _hasConnectedToHttpSource)
            {
                _productUpdateService.CheckForAvailableUpdateAsync();
            }
        }
    }
}

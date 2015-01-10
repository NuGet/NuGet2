using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
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
    public abstract class PackageListBaseCommand : NuGetPowerShellBaseCommand
    {
        public PackageListBaseCommand()
            : base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<SVsServiceProvider>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>())
        {
        }

        [Parameter]
        [Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public virtual int First { get; set; }

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

        protected abstract void Preprocess();

        protected override void ProcessRecordCore()
        {
        }

        /// <summary>
        /// Filter the installed packages list based on Filter, Skip and First parameters
        /// </summary>
        /// <returns></returns>
        protected Dictionary<VsProject, IEnumerable<JObject>> GetInstalledPackages(string filter, int skip, int take)
        {
            IEnumerable<VsProject> targets = GetProjects();
            Dictionary<VsProject, IEnumerable<JObject>> installedPackages = new Dictionary<VsProject, IEnumerable<JObject>>();

            foreach (VsProject project in targets)
            {
                Task<IEnumerable<JObject>> task = project.InstalledPackages.GetAllInstalledPackagesAndMetadata();
                IEnumerable<JObject> installedJObjects = task.Result;
                // Filter the results by string
                if (!string.IsNullOrEmpty(filter))
                {
                    installedJObjects = installedJObjects.Where(p => p.Value<string>(Properties.PackageId).StartsWith(filter, StringComparison.OrdinalIgnoreCase));
                }

                // Skip and then take
                installedJObjects = installedJObjects.Skip(skip);
                if (take != 0)
                {
                    installedJObjects = installedJObjects.Take(take);
                }
                installedPackages.Add(project, installedJObjects);
            }
            return installedPackages;
        }

        protected IEnumerable<JObject> GetPackagesFromRemoteSource(string packageId, IEnumerable<FrameworkName> names, bool allowPrerelease, int skip, int take)
        {
            IEnumerable<JObject> packages = Enumerable.Empty<JObject>();
            packages = PowerShellPackage.GetPackageVersions(ActiveSourceRepository, packageId, names, allowPrerelease, skip, take);
            return packages;
        }

        protected Dictionary<VsProject, IEnumerable<JObject>> GetPackageUpdatesFromRemoteSource(string filter, bool allowPrerelease, int skip, int take, bool allVersions)
        {
            IEnumerable<VsProject> targets = GetProjects();
            Dictionary<VsProject, IEnumerable<JObject>> packageUpdates = new Dictionary<VsProject, IEnumerable<JObject>>();

            foreach (VsProject project in targets)
            {
                Task<IEnumerable<JObject>> task = project.InstalledPackages.GetAllInstalledPackagesAndMetadata();
                IEnumerable<JObject> installedJObjects = task.Result;
                List<JObject> filteredUpdates = new List<JObject>();

                foreach (JObject jObject in installedJObjects)
                {
                    // Find packages update
                    List<JObject> updates = PowerShellPackage.GetLastestJObjectsForPackage(ActiveSourceRepository, jObject, project, allowPrerelease, skip, take, allVersions);
                    NuGetVersion originalVersion = PowerShellPackage.GetNuGetVersionFromString(jObject.Value<string>(Properties.Version));

                    if (!string.IsNullOrEmpty(filter))
                    {
                        updates = updates.Where(p => p.Value<string>(Properties.PackageId).StartsWith(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    filteredUpdates.AddRange(updates);
                }
                packageUpdates.Add(project, filteredUpdates);
            }
            return packageUpdates;
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

        protected void WritePackages(Dictionary<VsProject, IEnumerable<JObject>> dictionary, VersionType versionType)
        {
            // Get the PowerShellPackageView
            var view = PowerShellPackageWithProject.GetPowerShellPackageView(dictionary, versionType);
            if (view.IsEmpty())
            {
                if (UseRemoteSource)
                {
                    Log(MessageLevel.Info, Resources.Cmdlet_NoPackageUpdates);
                }
                else
                {
                    Log(MessageLevel.Info, Resources.Cmdlet_NoPackagesInstalled);
                }
            }
            WriteObject(view, enumerateCollection: true);
        }
    }
}

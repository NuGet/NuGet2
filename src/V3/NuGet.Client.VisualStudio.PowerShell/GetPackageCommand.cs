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
    public class GetPackageCommand : PackageListBaseCommand
    {
        public GetPackageCommand() :
            base()
        {
        }

        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

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

        protected override void BeginProcessing()
        {
            UseRemoteSourceOnly =  ListAvailable.IsPresent || (!String.IsNullOrEmpty(Source) && !Updates.IsPresent);
            UseRemoteSource = ListAvailable.IsPresent || Updates.IsPresent || !String.IsNullOrEmpty(Source);
            CollapseVersions = !AllVersions.IsPresent && ListAvailable; 
            base.BeginProcessing();
        }

        protected override void ProcessRecordCore()
        {
            // If Remote & Updates set of parameters are not specified
            if (!UseRemoteSource)
            {
                IEnumerable<JObject> installedPackages = FilterInstalledPackagesResults();
                WritePackages(installedPackages);
            }
            else
            {
                // Connect to remote source to get list of available packages or updates
            }
        }

        /// <summary>
        /// Filter the installed packages list based on Filter, Skip and First parameters
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<JObject> FilterInstalledPackagesResults()
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
            if (First != 0)
            {
                installedPackages = installedPackages.Take(First).ToList();
            }
            return installedPackages;
        }

        /// <summary>
        /// Get current projects or all projects in the solution
        /// </summary>
        /// <returns></returns>
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
    }
}
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.Versioning;

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
            base.TargetProjectName = this.ProjectName;
            UseRemoteSourceOnly =  ListAvailable.IsPresent || (!String.IsNullOrEmpty(Source) && !Updates.IsPresent);
            UseRemoteSource = ListAvailable.IsPresent || Updates.IsPresent || !String.IsNullOrEmpty(Source);
            CollapseVersions = !AllVersions.IsPresent && ListAvailable;
            if (UseRemoteSource || UseRemoteSourceOnly)
            {
                this.ActiveSourceRepository = GetActiveRepository(Source);
            }
            base.BeginProcessing();
        }

        protected override void ProcessRecordCore()
        {
            Preprocess();

            IEnumerable<JObject> packagesToDisplay = Enumerable.Empty<JObject>();
            // If Remote & Updates set of parameters are not specified
            if (!UseRemoteSource)
            {
                CheckForSolutionOpen();
                packagesToDisplay = FilterInstalledPackagesResults(Filter, Skip, First);
            }
            else
            {
                Log(MessageLevel.Warning, Resources.Cmdlet_CommandObsolete, "Find-Package -Id <string> [-Version <string>] [-ListAll] [-Latest]");

                // Connect to remote source to get list of available packages or updates
                if (First == 0)
                {
                    First = Int32.MaxValue;
                }

                if (UseRemoteSourceOnly)
                {
                    packagesToDisplay = GetPackagesFromRemoteSource(Filter, Enumerable.Empty<FrameworkName>(), IncludePrerelease.IsPresent, Skip, First, CollapseVersions);
                }
                // Get package updates from the remote source
                else
                {
                    CheckForSolutionOpen();
                    IEnumerable<JObject> updates = GetPackageUpdatesFromRemoteSource(IncludePrerelease.IsPresent, Skip, First);
                    packagesToDisplay = updates.Where(p => p.Value<string>(Properties.PackageId).StartsWith(Filter, StringComparison.OrdinalIgnoreCase));
                }

                // Output the list of package results to PowerShell console.
                WritePackages(packagesToDisplay);
            }
        }
    }
}
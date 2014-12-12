using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Runtime.Versioning;

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This command lists the available packages which are either from a package source or installed in the current solution.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Package", DefaultParameterSetName = ParameterAttribute.AllParameterSets)]
    [OutputType(typeof(IPackage))]
    public class GetPackageCommand : PackageListBaseCommand
    {
        private const int DefaultFirstValue = 50;
        private bool _enablePaging;

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

        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "Updates")]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Remote")]
        [Alias("Online", "Remote")]
        public SwitchParameter ListAvailable { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Updates")]
        public SwitchParameter Updates { get; set; }

        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "Updates")]
        public SwitchParameter AllVersions { get; set; }

        [Parameter(ParameterSetName = "Remote")]
        public int PageSize { get; set; }

        protected override void Preprocess()
        {
            base.TargetProjectName = this.ProjectName;
            UseRemoteSourceOnly = ListAvailable.IsPresent || (!String.IsNullOrEmpty(Source) && !Updates.IsPresent);
            UseRemoteSource = ListAvailable.IsPresent || Updates.IsPresent || !String.IsNullOrEmpty(Source);
            CollapseVersions = !AllVersions.IsPresent && ListAvailable;
            if (UseRemoteSource || UseRemoteSourceOnly)
            {
                this.ActiveSourceRepository = GetActiveRepository(Source);
            }
        }

        protected override void ProcessRecordCore()
        {
            Preprocess();

            // If Remote & Updates set of parameters are not specified
            if (!UseRemoteSource)
            {
                CheckForSolutionOpen();
                Dictionary<VsProject, IEnumerable<JObject>> packagesToDisplay = new Dictionary<VsProject, IEnumerable<JObject>>();
                packagesToDisplay = GetInstalledPackages(Filter, Skip, First);
                WritePackages(packagesToDisplay, VersionType.single);
            }
            else
            {
                if (PageSize != 0)
                {
                    _enablePaging = true;
                    First = PageSize;
                }
                else if (First == 0)
                {
                    First = DefaultFirstValue;
                }

                // Find avaiable packages from the online sources and not taking targetframeworks into account. 
                if (UseRemoteSourceOnly)
                {
                    IEnumerable<JObject> packagesToDisplay = Enumerable.Empty<JObject>();
                    // Display the First number of packages
                    packagesToDisplay = GetPackagesFromRemoteSource(Filter, Enumerable.Empty<FrameworkName>(), IncludePrerelease.IsPresent, Skip, First);
                    WritePackagesFromRemoteSources(packagesToDisplay, true);
                    if (_enablePaging)
                    {
                        WriteMoreRemotePackagesWithPaging(packagesToDisplay);
                    }
                }
                else
                {
                    // Get package updates from the remote source and take targetframeworks into account.
                    CheckForSolutionOpen();
                    Dictionary<VsProject, IEnumerable<JObject>> packagesToDisplay = new Dictionary<VsProject, IEnumerable<JObject>>();
                    packagesToDisplay = GetPackageUpdatesFromRemoteSource(Filter, IncludePrerelease.IsPresent, Skip, First, AllVersions.IsPresent);
                    WriteUpdatePackagesFromRemoteSource(packagesToDisplay);
                }
            }
        }

        private void WriteUpdatePackagesFromRemoteSource(Dictionary<VsProject, IEnumerable<JObject>> packagesToDisplay)
        {
            VersionType versionType;
            if (!CollapseVersions)
            {
                versionType = VersionType.all;
            }
            else
            {
                versionType = VersionType.single;
            }
            WritePackages(packagesToDisplay, versionType);
        }

        private void WritePackagesFromRemoteSources(IEnumerable<JObject> packagesToDisplay, bool outputWarning = false)
        {
            // Write warning message for Get-Package -ListAvaialble -Filter being obsolete
            // and will be replaced by Find-Package [-Id] 
            VersionType versionType;
            string message;
            if (!CollapseVersions)
            {
                versionType = VersionType.all;
                message = "Find-Package [-Id] -ListAll";
            }
            else
            {
                versionType = VersionType.latest;
                message = "Find-Package [-Id]";
            }

            // Output list of PowerShellPackages
            if (outputWarning && !string.IsNullOrEmpty(Filter))
            {
                Log(MessageLevel.Warning, Resources.Cmdlet_CommandObsolete, message);
            }

            WritePackages(packagesToDisplay, versionType);
        }

        private void WriteMoreRemotePackagesWithPaging(IEnumerable<JObject> packagesToDisplay)
        {
            // Display more packages with paging
            int pageNumber = 1;
            while (true)
            {
                packagesToDisplay = GetPackagesFromRemoteSource(Filter, Enumerable.Empty<FrameworkName>(), IncludePrerelease.IsPresent, pageNumber * PageSize, PageSize);
                if (packagesToDisplay.Count() != 0)
                {
                    // Prompt to user and if want to continue displaying more packages
                    int command = AskToContinueDisplayPackages();
                    if (command == 0)
                    {
                        // If yes, display the next page of (PageSize) packages
                        WritePackagesFromRemoteSources(packagesToDisplay);
                    }
                    else
                    {
                        break;
                    }
                }
                pageNumber++;
            }
        }

        private int AskToContinueDisplayPackages()
        {
            // Add a line before message prompt
            WriteLine();
            var choices = new Collection<ChoiceDescription>
            {
                new ChoiceDescription(Resources.Cmdlet_Yes, Resources.Cmdlet_DisplayMorePackagesYesHelp),
                new ChoiceDescription(Resources.Cmdlet_No, Resources.Cmdlet_DisplayMorePackagesNoHelp),
            };

            int choice = Host.UI.PromptForChoice(string.Empty, Resources.Cmdlet_PrompToDisplayMorePackages, choices, defaultChoice: 1);

            Debug.Assert(choice >= 0 && choice < 2);
            // Add a line after
            WriteLine();
            return choice;
        }
    }
}
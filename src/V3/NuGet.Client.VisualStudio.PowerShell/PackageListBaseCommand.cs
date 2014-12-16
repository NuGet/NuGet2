using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using NuGet.Client.Installation;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.PowerShell
{
    public class PackageListBaseCommand : NuGetPowerShellBaseCommand
    {
        private bool _hasConnectedToHttpSource;

        public PackageListBaseCommand()
            : base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<SVsServiceProvider>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>())
        {
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

        /// <summary>
        /// Determines if local repository are not needed to process this command
        /// </summary>
        protected bool UseRemoteSourceOnly { get; set; }

        /// <summary>
        /// Determines if a remote repository will be used to process this command.
        /// </summary>
        protected bool UseRemoteSource { get; set; }

        protected virtual bool CollapseVersions { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to display friendly message to the console.")]
        protected override void ProcessRecordCore()
        {
            try
            {
                CheckForSolutionOpen();
                Preprocess();
            }
            catch (Exception ex)
            {
                // unhandled exceptions should be terminating
                ErrorHandler.HandleException(ex, terminating: true);
            }
            finally
            {
                UnsubscribeEvents();
            }
        }

        protected virtual void Preprocess()
        {
            this.ActiveSourceRepository = GetActiveRepository(Source);
            //VsProject vsProject = GetProject(true);
            //this.Projects = new List<VsProject> { vsProject };
        }

        protected IEnumerable<JObject> GetPackagesFromRemoteSource()
        {
            //IEnumerable<JObject> packages = PowerShellPackage.GetAllVersionsForPackage();
            return null;
        }

        //private IProjectManager GetProjectManager(string projectName)
        //{
        //    Project project = SolutionManager.GetProject(projectName);
        //    if (project == null)
        //    {
        //        ErrorHandler.ThrowNoCompatibleProjectsTerminatingError();
        //    }
        //    IProjectManager projectManager = PackageManager.GetProjectManager(project);
        //    Debug.Assert(projectManager != null);

        //    return projectManager;
        //}

        protected virtual IEnumerable<IPackage> FilterPackages(IPackageRepository sourceRepository, IQueryable<IPackage> packages)
        {
            if (CollapseVersions)
            {
                // In the event the client is going up against a v1 feed, do not try to fetch pre release packages since this flag does not exist.
                if (IncludePrerelease && sourceRepository.SupportsPrereleasePackages)
                {
                    // Review: We should change this to show both the absolute latest and the latest versions but that requires changes to our collapsing behavior.
                    packages = packages.Where(p => p.IsAbsoluteLatestVersion);
                }
                else
                {
                    packages = packages.Where(p => p.IsLatestVersion);
                }
            }

            if (UseRemoteSourceOnly && First != 0)
            {
                // Optimization: If First parameter is specified, we'll wrap the IQueryable in a BufferedEnumerable to prevent consuming the entire result set.
                packages = packages.AsBufferedEnumerable(First * 3).AsQueryable();
            }

            IEnumerable<IPackage> packagesToDisplay = packages.AsEnumerable()
                                                              .Where(PackageExtensions.IsListed);

            // When querying a remote source, collapse versions unless AllVersions is specified.
            // We need to do this as the last step of the Queryable as the filtering occurs on the client.
            if (CollapseVersions)
            {
                // Review: We should perform the Listed check over OData for better perf
                packagesToDisplay = packagesToDisplay.AsCollapsed();
            }

            if (!IncludePrerelease) //&& ListAvailable
            {
                // If we aren't collapsing versions, and the pre-release flag is not set, only display release versions when displaying from a remote source.
                // We don't need to filter packages when showing installed packages.
                packagesToDisplay = packagesToDisplay.Where(p => p.IsReleaseVersion());
            }

            packagesToDisplay = packagesToDisplay.Skip(Skip);

            if (First != 0)
            {
                packagesToDisplay = packagesToDisplay.Take(First);
            }

            return packagesToDisplay;
        }

        protected void WritePackages(IEnumerable<JObject> packages)
        {
            // Get the PowerShellPackageView
            var view = PowerShellPackage.GetPowerShellPackageView(packages);
            WriteObject(view, enumerateCollection: true);
        }
    }
}

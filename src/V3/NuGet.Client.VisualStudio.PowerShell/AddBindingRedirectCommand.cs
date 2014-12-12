using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.Runtime;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.PowerShell
{
    [Cmdlet(VerbsCommon.Add, "BindingRedirect2")]
    [OutputType(typeof(AssemblyBinding))]
    public class AddBindingRedirectCommand : NuGetPowerShellBaseCommand
    {
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IVsFrameworkMultiTargeting _frameworkMultiTargeting;

        public AddBindingRedirectCommand()
            : base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<SVsServiceProvider>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>())
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi")]
        public AddBindingRedirectCommand(
            IFileSystemProvider fileSystemProvider,
            IVsFrameworkMultiTargeting frameworkMultiTargeting)
            : this()
        {
            _fileSystemProvider = fileSystemProvider;
            _frameworkMultiTargeting = frameworkMultiTargeting;
        }

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "PowerShell API requirement")]
        public string[] ProjectName { get; set; }

        protected override void ProcessRecordCore()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            var projects = new List<Project>();

            // if no project specified, use default
            if (ProjectName == null)
            {
                Project project = this.SolutionManager.DefaultProject;

                // if no default project (empty solution), throw terminating
                if (project == null)
                {
                    ErrorHandler.ThrowNoCompatibleProjectsTerminatingError();
                }

                projects.Add(project);
            }
            else
            {
                // get matching projects, expanding wildcards
                projects.AddRange(GetProjectsByName(ProjectName));
            }

            // Create a new app domain so we don't load the assemblies into the host app domain
            AppDomain domain = AppDomain.CreateDomain("domain");

            try
            {
                foreach (Project project in projects)
                {
                    var redirects = RuntimeHelpers.AddBindingRedirects(project, _fileSystemProvider, domain, _frameworkMultiTargeting);

                    // Print out what we did
                    WriteObject(redirects, enumerateCollection: true);
                }
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.Runtime;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "BindingRedirect")]
    [OutputType(typeof(AssemblyBinding))]
    public class AddBindingRedirectCommand : NuGetBaseCommand
    {
        private readonly ISolutionManager _solutionManager;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IVsFrameworkMultiTargeting _frameworkMultiTargeting;

        public AddBindingRedirectCommand()
            : this(
                ServiceLocator.GetInstance<ISolutionManager>(), 
                ServiceLocator.GetInstance<IHttpClientEvents>(), 
                ServiceLocator.GetInstance<IFileSystemProvider>(),
                ServiceLocator.GetGlobalService<SVsFrameworkMultiTargeting, IVsFrameworkMultiTargeting>())
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi")]
        public AddBindingRedirectCommand(
            ISolutionManager solutionManager, 
            IHttpClientEvents httpClientEvents, 
            IFileSystemProvider fileSystemProvider,
            IVsFrameworkMultiTargeting frameworkMultiTargeting)
            : base(solutionManager, null, httpClientEvents)
        {
            _solutionManager = solutionManager;
            _fileSystemProvider = fileSystemProvider;
            _frameworkMultiTargeting = frameworkMultiTargeting;
        }

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "PowerShell API requirement")]
        public string[] ProjectName { get; set; }

        protected override void ProcessRecordCore()
        {
            if (!_solutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            var projects = new List<Project>();

            // if no project specified, use default
            if (ProjectName == null)
            {
                Project project = _solutionManager.DefaultProject;

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
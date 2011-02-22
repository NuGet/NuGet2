using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

using EnvDTE;
using NuGet.Runtime;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands {
    [Cmdlet(VerbsCommon.Add, "BindingRedirect")]
    [OutputType(typeof(AssemblyBinding))]
    public class AddBindingRedirectCommand : NuGetBaseCommand {
        private readonly ISolutionManager _solutionManager;

        public AddBindingRedirectCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(), ServiceLocator.GetInstance<IVsProgressEvents>()) {
        }

        public AddBindingRedirectCommand(ISolutionManager solutionManager, IVsProgressEvents progressEvents)
            : base(solutionManager, null, progressEvents) {
            _solutionManager = solutionManager;
        }

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "PowerShell API requirement")]
        public string[] ProjectName { get; set; }

        protected override void ProcessRecordCore() {
            if (!_solutionManager.IsSolutionOpen) {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            var projects = new List<Project>();

            // if no project specified, use default
            if (ProjectName == null) {
                Project project = _solutionManager.DefaultProject;

                // if no default project (empty solution), throw terminating
                if (project == null) {
                    ErrorHandler.ThrowNoCompatibleProjectsTerminatingError();
                }

                projects.Add(project);
            }
            else {
                // get matching projects, expanding wildcards
                projects.AddRange(GetProjectsByName(ProjectName));
            }

            // Create a new app domain so we don't load the assemblies into the host app domain
            AppDomain domain = AppDomain.CreateDomain("domain");

            try {
                foreach (Project project in projects) {
                    // Get the project's output path
                    string outputPath = project.GetOutputPath();

                    // Get the binding redirects from the output path
                    IEnumerable<AssemblyBinding> redirects = BindingRedirectResolver.GetBindingRedirects(outputPath,
                                                                                                         domain);
                    // Create a project system
                    IFileSystem fileSystem = VsProjectSystemFactory.CreateProjectSystem(project);

                    // Create a binding redirect manager over the configuration
                    var manager = new BindingRedirectManager(fileSystem, project.GetConfigurationFile());

                    // Add the redirects
                    manager.AddBindingRedirects(redirects);

                    // Print out what we did
                    WriteObject(redirects, enumerateCollection: true);
                }
            }
            finally {
                AppDomain.Unload(domain);
            }
        }
    }
}

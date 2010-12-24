using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using EnvDTE;
using NuGet.Runtime;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio.Cmdlets {
    [Cmdlet(VerbsCommon.Add, "BindingRedirect")]
    public class AddBindingRedirectCmdlet : Cmdlet {        
        private readonly ISolutionManager _solutionManager;

        public AddBindingRedirectCmdlet()
            : this(ServiceLocator.GetInstance<ISolutionManager>()) {
        }

        public AddBindingRedirectCmdlet(ISolutionManager solutionManager) {
            _solutionManager = solutionManager;
        }

        [Parameter(Position = 0)]
        public string Project { get; set; }

        protected override void ProcessRecord() {
            if (!_solutionManager.IsSolutionOpen) {
                WriteError(VsResources.Cmdlet_NoSolution);
                return;
            }

            // Get the specified project
            Project project = GetProject();

            // Create a new app domain so we don't load the assemblies into the host app domain
            AppDomain domain = AppDomain.CreateDomain("domain");

            try {
                // Get the project's output path
                string outputPath = project.GetOutputPath();

                // Get the binding redirects from the output path
                IEnumerable<AssemblyBinding> redirects = BindingRedirectResolver.GetBindingRedirects(outputPath, domain);
                
                // Create a project system
                IFileSystem fileSystem = VsProjectSystemFactory.CreateProjectSystem(project);

                // Create a binding redirect manager over the configuration
                var manager = new BindingRedirectManager(fileSystem, project.GetConfigurationFile());

                // Add the redirects
                manager.AddBindingRedirects(redirects);

                // Print out what we did
                WriteObject(redirects, enumerateCollection: true);
            }
            finally {
                AppDomain.Unload(domain);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "This exception is passed to PowerShell. We really don't care about the type of exception here.")]
        protected void WriteError(string message) {
            if (!String.IsNullOrEmpty(message)) {
                WriteError(new Exception(message));
            }
        }

        protected void WriteError(Exception exception) {
            WriteError(new ErrorRecord(exception, String.Empty, ErrorCategory.NotSpecified, null));
        }

        private Project GetProject() {
            Debug.Assert(_solutionManager.IsSolutionOpen);

            Project project = null;
            if (!String.IsNullOrEmpty(Project)) {
                project = _solutionManager.GetProject(Project);
            }

            // If project is null fall back to the default project
            project = project ?? _solutionManager.DefaultProject;

            if (project == null) {
                throw new InvalidOperationException(VsResources.Cmdlet_MissingProjectParameter);
            }

            return project;
        }
    }
}

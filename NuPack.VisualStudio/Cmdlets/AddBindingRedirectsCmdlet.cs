using System;
using System.Collections.Generic;
using System.Management.Automation;
using EnvDTE;
using NuGet.Runtime;

namespace NuGet.VisualStudio.Cmdlets {
    [Cmdlet(VerbsCommon.Add, "BindingRedirects")]
    public class AddBindingRedirectsCmdlet : Cmdlet {
        private const string WebConfig = "web.config";
        private const string AppConfig = "app.config";

        private readonly ISolutionManager _solutionManager;

        public AddBindingRedirectsCmdlet()
            : this(ServiceLocator.GetInstance<ISolutionManager>()) {

        }

        public AddBindingRedirectsCmdlet(ISolutionManager solutionManager) {
            _solutionManager = solutionManager;
        }

        [Parameter(Position = 0)]
        public string Name { get; set; }

        protected override void ProcessRecord() {
            Project project = _solutionManager.GetProject(Name ?? _solutionManager.DefaultProjectName);

            // Create a new app domain so we don't load the assemblies into the host app domain
            AppDomain domain = AppDomain.CreateDomain("domain");

            try {
                // Get the project's output pth
                string outputPath = project.GetOutputPath();

                // Create a project system for this package
                ProjectSystem projectSystem = VsProjectSystemFactory.CreateProjectSystem(project);
                IEnumerable<BindingRedirect> redirects = BindingRedirectResolver.GetBindingRedirects(outputPath, domain);

                string configFile = AppConfig;
                if (project.IsWebProject()) {
                    configFile = WebConfig;
                }

                var manager = new BindingRedirectManager(projectSystem, configFile);

                manager.AddBindingRedirects(redirects);

                WriteObject(redirects, enumerateCollection: true);
            }
            finally {
                AppDomain.Unload(domain);
            }
        }
    }
}

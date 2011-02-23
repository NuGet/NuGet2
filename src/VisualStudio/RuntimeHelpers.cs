using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuGet.Runtime;

namespace NuGet.VisualStudio {
    public static class RuntimeHelpers {
        public static IEnumerable<AssemblyBinding> AddBindingRedirects(Project project, AppDomain domain) {
            var redirects = Enumerable.Empty<AssemblyBinding>();
            // Only add binding redirects to projects that aren't class libraries
            if (!project.IsClassLibrary()) {
                // Create a project system
                IFileSystem fileSystem = VsProjectSystemFactory.CreateProjectSystem(project);

                IEnumerable<string> assemblies = ThreadHelper.Generic.Invoke(() => project.GetAssemblyClosure());

                redirects = BindingRedirectResolver.GetBindingRedirects(assemblies, domain);

                // Create a binding redirect manager over the configuration
                var manager = new BindingRedirectManager(fileSystem, project.GetConfigurationFile());

                // Add the redirects
                manager.AddBindingRedirects(redirects);
            }

            return redirects;
        }

        public static void AddBindingRedirects(ISolutionManager solutionManager, Project project) {
            // Create a new app domain so we can load the assemblies without locking them in this app domain
            AppDomain domain = AppDomain.CreateDomain("assembliesDomain");

            try {
                // Keep track of visited projects
                AddBindingRedirects(solutionManager, project, domain);
            }
            finally {
                AppDomain.Unload(domain);
            }
        }

        public static void AddBindingRedirects(ISolutionManager solutionManager, Project project, AppDomain domain) {
            var projects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddBindingRedirects(solutionManager, project, domain, projects);
        }

        private static void AddBindingRedirects(ISolutionManager solutionManager, Project project, AppDomain domain, HashSet<string> projects) {
            if (projects.Contains(project.UniqueName)) {
                return;
            }

            AddBindingRedirects(project, domain);

            // Add binding redirects too all projects that are referencing this one
            foreach (Project dependentProject in solutionManager.GetDependentProjects(project)) {
                AddBindingRedirects(solutionManager, dependentProject, domain, projects);
            }

            projects.Add(project.UniqueName);
        }        
    }
}

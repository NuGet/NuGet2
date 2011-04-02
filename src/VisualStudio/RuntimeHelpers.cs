using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuGet.Runtime;

namespace NuGet.VisualStudio {
    public static class RuntimeHelpers {
        public static IEnumerable<AssemblyBinding> AddBindingRedirects(Project project, AppDomain domain) {
            if (project.SupportsBindingRedirects()) {
                return AddBindingRedirects(project, domain, new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase));
            }

            return Enumerable.Empty<AssemblyBinding>();
        }

        private static IEnumerable<AssemblyBinding> AddBindingRedirects(Project project, AppDomain domain, IDictionary<string, HashSet<string>> projectAssembliesCache) {
            var redirects = Enumerable.Empty<AssemblyBinding>();
            // Only add binding redirects to projects that aren't class libraries
            if (project.SupportsConfig()) {
                // Create a project system
                IFileSystem fileSystem = VsProjectSystemFactory.CreateProjectSystem(project);

                // Run this on the UI thread since it enumerates all references
                IEnumerable<string> assemblies = ThreadHelper.Generic.Invoke(() => project.GetAssemblyClosure(projectAssembliesCache));

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
                if (project.SupportsBindingRedirects()) {
                    AddBindingRedirects(solutionManager, project, domain);
                }
            }
            finally {
                AppDomain.Unload(domain);
            }
        }

        private static void AddBindingRedirects(ISolutionManager solutionManager, Project project, AppDomain domain) {
            var visitedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var projectAssembliesCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            AddBindingRedirects(solutionManager, project, domain, visitedProjects, projectAssembliesCache);
        }

        private static void AddBindingRedirects(ISolutionManager solutionManager, Project project, AppDomain domain, HashSet<string> projects, IDictionary<string, HashSet<string>> projectAssembliesCache) {
            if (projects.Contains(project.UniqueName)) {
                return;
            }

            AddBindingRedirects(project, domain, projectAssembliesCache);

            // Add binding redirects to all projects that are referencing this one
            foreach (Project dependentProject in solutionManager.GetDependentProjects(project)) {
                AddBindingRedirects(solutionManager, dependentProject, domain, projects, projectAssembliesCache);
            }

            projects.Add(project.UniqueName);
        }
    }
}

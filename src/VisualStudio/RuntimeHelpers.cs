using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuGet.Runtime;

namespace NuGet.VisualStudio
{
    public static class RuntimeHelpers
    {
        public static IEnumerable<AssemblyBinding> AddBindingRedirects(Project project, IFileSystemProvider fileSystemProvider, AppDomain domain)
        {
            if (project.SupportsBindingRedirects())
            {
                // When we're adding binding redirects explicitly, don't check the project type
                return AddBindingRedirects(project, fileSystemProvider, domain, new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase), checkProjectType: false);
            }

            return Enumerable.Empty<AssemblyBinding>();
        }

        private static IEnumerable<AssemblyBinding> AddBindingRedirects(Project project, IFileSystemProvider fileSystemProvider, AppDomain domain, IDictionary<string, HashSet<string>> projectAssembliesCache, bool checkProjectType = true)
        {
            var redirects = Enumerable.Empty<AssemblyBinding>();
            // Only add binding redirects to projects that aren't class libraries
            if (!checkProjectType || project.SupportsConfig())
            {
                // Create a project system
                IFileSystem fileSystem = VsProjectSystemFactory.CreateProjectSystem(project, fileSystemProvider);

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

        public static void AddBindingRedirects(ISolutionManager solutionManager, Project project, IFileSystemProvider fileSystemProvider)
        {
            // Create a new app domain so we can load the assemblies without locking them in this app domain
            AppDomain domain = AppDomain.CreateDomain("assembliesDomain");

            try
            {
                // Keep track of visited projects
                if (project.SupportsBindingRedirects())
                {
                    AddBindingRedirects(solutionManager, project, fileSystemProvider, domain);
                }
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        private static void AddBindingRedirects(ISolutionManager solutionManager, Project project, IFileSystemProvider fileSystemProvider, AppDomain domain)
        {
            var visitedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var projectAssembliesCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            AddBindingRedirects(solutionManager, project, fileSystemProvider, domain, visitedProjects, projectAssembliesCache);
        }

        private static void AddBindingRedirects(ISolutionManager solutionManager, Project project, IFileSystemProvider fileSystemProvider, AppDomain domain, HashSet<string> projects, IDictionary<string, HashSet<string>> projectAssembliesCache)
        {
            if (projects.Contains(project.UniqueName))
            {
                return;
            }

            AddBindingRedirects(project, fileSystemProvider, domain, projectAssembliesCache);

            // Add binding redirects to all projects that are referencing this one
            foreach (Project dependentProject in solutionManager.GetDependentProjects(project))
            {
                AddBindingRedirects(solutionManager, dependentProject, fileSystemProvider, domain, projects, projectAssembliesCache);
            }

            projects.Add(project.UniqueName);
        }
    }
}

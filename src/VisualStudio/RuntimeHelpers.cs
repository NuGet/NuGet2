using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Runtime.Versioning;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.Runtime;

namespace NuGet.VisualStudio
{
    public static class RuntimeHelpers
    {
        public static IEnumerable<AssemblyBinding> AddBindingRedirects(
            Project project, 
            IFileSystemProvider fileSystemProvider, 
            AppDomain domain,
            IVsFrameworkMultiTargeting frameworkMultiTargeting)
        {
            if (project.SupportsBindingRedirects())
            {
                // When we're adding binding redirects explicitly, don't check the project type
                return AddBindingRedirects(
                    project, 
                    fileSystemProvider, 
                    domain, 
                    new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase), 
                    frameworkMultiTargeting,
                    checkProjectType: false);
            }

            return Enumerable.Empty<AssemblyBinding>();
        }

        private static IEnumerable<AssemblyBinding> AddBindingRedirects(
            Project project, 
            IFileSystemProvider fileSystemProvider, 
            AppDomain domain, 
            IDictionary<string, HashSet<string>> projectAssembliesCache, 
            IVsFrameworkMultiTargeting frameworkMultiTargeting,
            bool checkProjectType = true)
        {
            var redirects = Enumerable.Empty<AssemblyBinding>();
            // Only add binding redirects to projects that aren't class libraries
            if (!checkProjectType || project.SupportsConfig())
            {
                // Create a project system
                IFileSystem fileSystem = VsProjectSystemFactory.CreateProjectSystem(project, fileSystemProvider);

                // Run this on the UI thread since it enumerates all references
                IEnumerable<string> assemblies = ThreadHelper.Generic.Invoke(() => project.GetAssemblyClosure(fileSystemProvider, projectAssembliesCache));

                redirects = BindingRedirectResolver.GetBindingRedirects(assemblies, domain);

                if (frameworkMultiTargeting != null)
                {
                    // filter out assemblies that already exist in the target framework (CodePlex issue #3072)
                    FrameworkName targetFrameworkName = project.GetTargetFrameworkName();
                    redirects = redirects.Where(p => !FrameworkAssemblyResolver.IsHigherAssemblyVersionInFramework(p.Name, p.AssemblyNewVersion, targetFrameworkName, fileSystemProvider));
                }

                // Create a binding redirect manager over the configuration
                var manager = new BindingRedirectManager(fileSystem, project.GetConfigurationFile());

                // Add the redirects
                manager.AddBindingRedirects(redirects);
            }

            return redirects;
        }

        public static void AddBindingRedirects(
            ISolutionManager solutionManager, 
            Project project, 
            IFileSystemProvider fileSystemProvider, 
            IVsFrameworkMultiTargeting frameworkMultiTargeting)
        {
            // Create a new app domain so we can load the assemblies without locking them in this app domain
            AppDomain domain = AppDomain.CreateDomain("assembliesDomain");

            try
            {
                // Keep track of visited projects
                if (project.SupportsBindingRedirects())
                {
                    AddBindingRedirects(solutionManager, project, fileSystemProvider, domain, frameworkMultiTargeting);
                }
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        private static void AddBindingRedirects(
            ISolutionManager solutionManager, 
            Project project, 
            IFileSystemProvider fileSystemProvider, 
            AppDomain domain,
            IVsFrameworkMultiTargeting frameworkMultiTargeting)
        {
            var visitedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var projectAssembliesCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            AddBindingRedirects(solutionManager, project, fileSystemProvider, domain, visitedProjects, projectAssembliesCache, frameworkMultiTargeting);
        }

        private static void AddBindingRedirects(
            ISolutionManager solutionManager, 
            Project project, 
            IFileSystemProvider fileSystemProvider, 
            AppDomain domain, 
            HashSet<string> projects, 
            IDictionary<string, HashSet<string>> projectAssembliesCache,
            IVsFrameworkMultiTargeting frameworkMultiTargeting)
        {
            string projectUniqueName = project.GetUniqueName();
            if (projects.Contains(projectUniqueName))
            {
                return;
            }

            if (project.SupportsBindingRedirects())
            {
                AddBindingRedirects(project, fileSystemProvider, domain, projectAssembliesCache, frameworkMultiTargeting);
            }

            // Add binding redirects to all projects that are referencing this one
            foreach (Project dependentProject in solutionManager.GetDependentProjects(project))
            {
                AddBindingRedirects(
                    solutionManager, 
                    dependentProject, 
                    fileSystemProvider, 
                    domain, 
                    projects, 
                    projectAssembliesCache, 
                    frameworkMultiTargeting);
            }

            projects.Add(projectUniqueName);
        }

        /// <summary>
        /// Load the specified assembly using the information from the executing assembly. 
        /// If the executing assembly is strongly signed, use Assembly.Load(); Otherwise, 
        /// use Assembly.LoadFrom()
        /// </summary>
        /// <param name="assemblyName">The name of the assembly to be loaded.</param>
        /// <returns>The loaded Assembly instance.</returns>
        internal static Assembly LoadAssemblySmart(string assemblyName)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            AssemblyName executingAssemblyName = executingAssembly.GetName();
            if (HasStrongName(executingAssemblyName))
            {
                // construct the Full Name of the assembly using the same version/culture/public key token 
                // of the executing assembly.
                string assemblyFullName = String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}, Version={1}, Culture=neutral, PublicKeyToken={2}",
                    assemblyName,
                    executingAssemblyName.Version.ToString(),
                    ConvertToHexString(executingAssemblyName.GetPublicKeyToken()));

                return Assembly.Load(assemblyFullName);
            }
            else
            {
                var assemblyDirectory = Path.GetDirectoryName(executingAssembly.Location);
                return Assembly.LoadFrom(Path.Combine(assemblyDirectory, assemblyName + ".dll"));
            }
        }

        private static bool HasStrongName(AssemblyName assembly)
        {
            byte[] publicKeyToken = assembly.GetPublicKeyToken();
            return publicKeyToken != null && publicKeyToken.Length > 0;
        }

        private static string ConvertToHexString(byte[] data)
        {
            return new SoapHexBinary(data).ToString();
        }
    }
}
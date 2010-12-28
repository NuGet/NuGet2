using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.Runtime {
    public static class BindingRedirectResolver {
        /// <summary>
        /// Returns a list of assemblies that need binding redirects for a set of assemblies in a path.
        /// </summary>
        /// <param name="path">The directory where assemblies are.</param>
        public static IEnumerable<AssemblyBinding> GetBindingRedirects(string path) {
            return GetBindingRedirects(path, AppDomain.CurrentDomain);
        }

        /// <summary>
        /// Returns a list of assemblies that need binding redirects for a set of assemblies in a path.
        /// </summary>        
        /// <param name="path">The directory where assemblies are.</param>
        /// <param name="domain">The application domain to load the assemblies into.</param>
        public static IEnumerable<AssemblyBinding> GetBindingRedirects(string path, AppDomain domain) {
            if (path == null) {
                throw new ArgumentNullException("path");
            }

            if (domain == null) {
                throw new ArgumentNullException("domain");
            }

            return GetBindingRedirects(GetAssemblies(path, domain));
        }

        /// <summary>
        /// Returns a list of assemblies that need binding redirects.
        /// </summary>
        /// <param name="assemblies">List assemblies to analyze for binding redirects</param>
        public static IEnumerable<AssemblyBinding> GetBindingRedirects(IEnumerable<IAssembly> assemblies) {
            if (assemblies == null) {
                throw new ArgumentNullException("assemblies");
            }

            var assemblyNameLookup = assemblies.ToDictionary(GetUniqueKey);

            // Output set of assemblies we need redirects for 
            var redirectAssemblies = new HashSet<IAssembly>();

            // For each available assembly
            foreach (IAssembly assembly in assemblies) {
                foreach (IAssembly referenceAssembly in assembly.ReferencedAssemblies) {
                    Tuple<string, string> key = GetUniqueKey(referenceAssembly);
                    IAssembly targetAssembly;
                    // If we have an assembly with the same unique key in our list of a different version then we want to use that version
                    // then we want to add a redirect for that assembly
                    if (assemblyNameLookup.TryGetValue(key, out targetAssembly) && targetAssembly.Version != referenceAssembly.Version) {
                        redirectAssemblies.Add(targetAssembly);
                    }
                }
            }

            return redirectAssemblies.Select(a => new AssemblyBinding(a));
        }

        /// <summary>
        /// Returns the key for an assembly (name, public key)
        /// </summary>
        private static Tuple<string, string> GetUniqueKey(IAssembly assembly) {
            return Tuple.Create(assembly.Name, assembly.PublicKeyToken);
        }

        private static IEnumerable<IAssembly> GetAssemblies(string path, AppDomain domain) {
            foreach (var assemblyFile in Directory.GetFiles(path, "*.dll")) {
                yield return RemoteAssembly.LoadAssembly(assemblyFile, domain);
            }

            foreach (var assemblyFile in Directory.GetFiles(path, "*.exe")) {
                yield return RemoteAssembly.LoadAssembly(assemblyFile, domain);
            }
        }

    }
}

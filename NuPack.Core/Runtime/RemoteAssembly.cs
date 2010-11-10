using System;
using System.Collections.Generic;
using System.Reflection;

namespace NuGet.Runtime {
    /// <summary>
    /// IAssembly implementation that is used for marshalling information across app domains.
    /// </summary>
    internal class RemoteAssembly : MarshalByRefObject, IAssembly {
        private readonly List<IAssembly> _referencedAssemblies = new List<IAssembly>();

        public string Name {
            get;
            private set;
        }

        public Version Version {
            get;
            private set;
        }

        public string PublicKeyToken {
            get;
            private set;
        }

        public string Culture {
            get;
            private set;
        }

        public IEnumerable<IAssembly> ReferencedAssemblies {
            get {
                return _referencedAssemblies;
            }
        }

        public void Load(string path) {
            // Load the assembly in a reflection only context
            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(path);

            // Get the assembly name and set the properties on this object
            CopyAssemblyProperties(assembly.GetName(), this);

            // Do the same for referenced assemblies
            foreach (AssemblyName referencedAssemblyName in assembly.GetReferencedAssemblies()) {
                // Copy the properties to the referenced assembly
                var referencedAssembly = new RemoteAssembly();
                _referencedAssemblies.Add(CopyAssemblyProperties(referencedAssemblyName, referencedAssembly));
            }
        }

        private static RemoteAssembly CopyAssemblyProperties(AssemblyName assemblyName, RemoteAssembly assembly) {
            assembly.Name = assemblyName.Name;
            assembly.Version = assemblyName.Version;
            assembly.PublicKeyToken = assemblyName.GetPublicKeyTokenString();
            string culture = assemblyName.CultureInfo.ToString();

            if (String.IsNullOrEmpty(culture)) {
                assembly.Culture = "neutral";
            }
            else {
                assembly.Culture = culture;
            }

            return assembly;
        }

        internal static IAssembly LoadAssembly(string path, AppDomain domain) {
            if (domain != AppDomain.CurrentDomain) {
                var crossDomainAssembly = domain.CreateInstance<RemoteAssembly>();
                crossDomainAssembly.Load(path);

                return crossDomainAssembly;
            }

            // We never shut down the main app domain so just return the assembly
            var assembly = new RemoteAssembly();
            assembly.Load(path);
            return assembly;
        }
    }
}

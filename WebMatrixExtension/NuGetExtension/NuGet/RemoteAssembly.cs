using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet.Runtime;

namespace NuGet.WebMatrix
{
    // We need to make changes to NuGet.Core once we ship Beta so that this type is removed.
    internal class RemoteAssembly : MarshalByRefObject, IAssembly, IEquatable<RemoteAssembly>, IComparable<RemoteAssembly>
    {
        private readonly List<IAssembly> _referencedAssemblies = new List<IAssembly>();

        public RemoteAssembly()
        {
        }

        public RemoteAssembly(string name, Version version, string publicKeyToken, string culture)
        {
            Name = name;
            Version = version;
            PublicKeyToken = publicKeyToken;
            Culture = culture;
        }

        public string Name { get; private set; }

        public Version Version { get; private set; }

        public string PublicKeyToken { get; private set; }

        public string Culture { get; private set; }

        public IEnumerable<IAssembly> ReferencedAssemblies
        {
            get { return _referencedAssemblies; }
        }

        public void Load(string assemblyString, bool isPath)
        {
            Assembly assembly;

            if (isPath)
            {
                assembly = Assembly.ReflectionOnlyLoadFrom(assemblyString);
            }
            else
            {
                assembly = Assembly.ReflectionOnlyLoad(assemblyString);
            }

            // Get the assembly name and set the properties on this object
            CopyAssemblyProperties(assembly.GetName(), this);

            // Do the same for referenced assemblies
            foreach (AssemblyName referencedAssemblyName in assembly.GetReferencedAssemblies())
            {
                // Copy the properties to the referenced assembly
                var referencedAssembly = new RemoteAssembly();
                _referencedAssemblies.Add(CopyAssemblyProperties(referencedAssemblyName, referencedAssembly));
            }
        }

        private static RemoteAssembly CopyAssemblyProperties(AssemblyName assemblyName, RemoteAssembly assembly)
        {
            assembly.Name = assemblyName.Name;
            assembly.Version = assemblyName.Version;
            assembly.PublicKeyToken = assemblyName.GetPublicKeyTokenString();
            string culture = assemblyName.CultureInfo.ToString();

            if (String.IsNullOrEmpty(culture))
            {
                assembly.Culture = "neutral";
            }
            else
            {
                assembly.Culture = culture;
            }

            return assembly;
        }

        internal static IAssembly LoadAssembly(string assemblyString, AppDomain domain, bool isPath)
        {
            if (domain != AppDomain.CurrentDomain)
            {
                var pathToDLL = Assembly.GetExecutingAssembly().CodeBase;
                var typeName = typeof(RemoteAssembly).FullName;
                var crossDomainAssembly = (RemoteAssembly)domain.CreateInstanceFromAndUnwrap(pathToDLL, typeName);
                crossDomainAssembly.Load(assemblyString, isPath);
                return crossDomainAssembly;
            }

            var assembly = new RemoteAssembly();
            assembly.Load(assemblyString, isPath);
            return assembly;
        }

        public static IEnumerable<IAssembly> GetAssembliesForBindingRedirect(AppDomain appDomain, string binDirectoryPath)
        {
            return GetAssembliesForBindingRedirect(appDomain, binDirectoryPath, GetBinAssemblies);
        }

        internal static IEnumerable<IAssembly> GetAssembliesForBindingRedirect(AppDomain appDomain, string binDirectoryPath, Func<AppDomain, string, IEnumerable<IAssembly>> getBinAssemblies)
        {
            var binAssemblies = getBinAssemblies(appDomain, binDirectoryPath).ToList();
            if (!binAssemblies.Any())
            {
                return binAssemblies;
            }

            try
            {
                // the call to WebPagesDeployment might throw when webpages is not installed
                // this is OK -- we don't need to add binding redirects for web pages if its
                // not installed
                var gacAssemblies = WebPagesDeployment.GetWebPagesAssemblies()
                    .Select(a => LoadAssembly(a.FullName, appDomain, isPath: false));

                // we want to prefer an assembly from bin when the names match
                return binAssemblies.Concat(gacAssemblies)
                    .Distinct(new AssemblyNameComparer())
                    .ToArray();
            }
            catch
            {
                return binAssemblies
                    .Distinct(new AssemblyNameComparer())
                    .ToArray();
            }
        }

        private static IEnumerable<IAssembly> GetBinAssemblies(AppDomain appDomain, string binDirectoryPath)
        {
            if (!Directory.Exists(binDirectoryPath))
            {
                yield break;
            }

            var extensions = new[] { "*.dll", "*.exe" };
            foreach (var extension in extensions)
            {
                foreach (var path in Directory.EnumerateFiles(binDirectoryPath, extension))
                {
                    IAssembly assembly = LoadAssemblyFromSafe(appDomain, path);
                    if (assembly != null)
                    {
                        yield return assembly;
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We're loading arbitrary binaries from the bin and some of them might not be native. Catch all to prevent this from throwing.")]
        private static IAssembly LoadAssemblyFromSafe(AppDomain appDomain, string path)
        {
            try
            {
                return LoadAssembly(path, appDomain, isPath: true);
            }
            catch
            {
                // We don't want to throw from this method.
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            var otherAssembly = obj as RemoteAssembly;
            return otherAssembly != null && Equals(otherAssembly);
        }

        public bool Equals(RemoteAssembly other)
        {
            return CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public int CompareTo(RemoteAssembly other)
        {
            return Compare(this, other);
        }

        internal static int Compare(IAssembly a, IAssembly b)
        {
            if (b == null)
            {
                return 1;
            }

            var nameDiff = StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
            if (nameDiff != 0)
            {
                return nameDiff;
            }

            var versionDiff = a.Version.CompareTo(b.Version);
            if (versionDiff != 0)
            {
                return versionDiff;
            }

            var publicKeyDiff = StringComparer.OrdinalIgnoreCase.Compare(a.PublicKeyToken, b.PublicKeyToken);
            if (publicKeyDiff != 0)
            {
                return publicKeyDiff;
            }

            return StringComparer.OrdinalIgnoreCase.Compare(a.Culture, b.Culture);
        }

        private class AssemblyNameComparer : IEqualityComparer<IAssembly>
        {
            public bool Equals(IAssembly x, IAssembly y)
            {
                return x.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(IAssembly obj)
            {
                return obj.Name == null ? 0 : obj.Name.GetHashCode();
            }
        }
    }
}

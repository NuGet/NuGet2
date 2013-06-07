using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NuGet.Runtime;

namespace NuGet
{
    public static class AssemblyMetadataExtractor
    {
        public static AssemblyMetadata GetMetadata(string assemblyPath)
        {
            var setup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
            };

            AppDomain domain = AppDomain.CreateDomain("metadata", AppDomain.CurrentDomain.Evidence, setup);
            try
            {
                var extractor = domain.CreateInstance<MetadataExtractor>();
                return extractor.GetMetadata(assemblyPath);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        public static void ExtractMetadata(PackageBuilder builder, string assemblyPath)
        {
            AssemblyMetadata assemblyMetadata = GetMetadata(assemblyPath);
            builder.Id = assemblyMetadata.Name;
            builder.Version = assemblyMetadata.Version;
            builder.Title = assemblyMetadata.Title;
            builder.Description = assemblyMetadata.Description;
            builder.Copyright = assemblyMetadata.Copyright;

            if (!builder.Authors.Any() && !String.IsNullOrEmpty(assemblyMetadata.Company))
            {
                builder.Authors.Add(assemblyMetadata.Company);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "It's constructed using CreateInstanceAndUnwrap in another app domain")]
        private sealed class MetadataExtractor : MarshalByRefObject
        {
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "It's a marshal by ref object used to collection information in another app domain")]
            public AssemblyMetadata GetMetadata(string path)
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;

                try
                {
                    Assembly assembly = Assembly.ReflectionOnlyLoadFrom(path);
                    AssemblyName assemblyName = assembly.GetName();

                    var attributes = CustomAttributeData.GetCustomAttributes(assembly);

                    SemanticVersion version;
                    string assemblyInformationalVersion = GetAttributeValueOrDefault<AssemblyInformationalVersionAttribute>(attributes);
                    if (!SemanticVersion.TryParse(assemblyInformationalVersion, out version))
                    {
                        version = new SemanticVersion(assemblyName.Version);
                    }

                    return new AssemblyMetadata
                    {
                        Name = assemblyName.Name,
                        Version = version,
                        Title = GetAttributeValueOrDefault<AssemblyTitleAttribute>(attributes),
                        Company = GetAttributeValueOrDefault<AssemblyCompanyAttribute>(attributes),
                        Description = GetAttributeValueOrDefault<AssemblyDescriptionAttribute>(attributes),
                        Copyright = GetAttributeValueOrDefault<AssemblyCopyrightAttribute>(attributes)
                    };
                }
                finally
                {
                    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= ReflectionOnlyAssemblyResolve;
                }
            }

            private static Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
            {
                return Assembly.ReflectionOnlyLoad(args.Name);
            }

            private static string GetAttributeValueOrDefault<T>(IList<CustomAttributeData> attributes) where T : Attribute
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeType == typeof(T))
                    {
                        string value = attribute.ConstructorArguments[0].Value.ToString();
                        // Return the value only if it isn't null or empty so that we can use ?? to fall back
                        if (!String.IsNullOrEmpty(value))
                        {
                            return value;
                        }
                    }
                }
                return null;
            }
        }
    }
}

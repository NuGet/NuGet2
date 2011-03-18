using System;
using System.Linq;
using System.Reflection;

namespace NuGet.Common {
    public static class AssemblyMetadataExtractor {
        public static void ExtractMetadata(string assemblyPath, PackageBuilder builder) {
            // Load the assembly and try to read the attributes from them
            // REVIEW: ReflectionOnlyLoad would probably be better but we have to read attributes
            // using GetCustomAttributeData() which is a bit wonky
            var assembly = new Lazy<Assembly>(() => Assembly.LoadFrom(assemblyPath));
            AssemblyName assemblyName = assembly.Value.GetName();
            builder.Id = builder.Id ?? assemblyName.Name;
            builder.Version = builder.Version ?? assemblyName.Version;
            builder.Title = builder.Title ?? GetAttributeValueOrDefault<AssemblyTitleAttribute>(assembly.Value, a => a.Title);
            builder.Description = builder.Description ?? GetAttributeValueOrDefault<AssemblyDescriptionAttribute>(assembly.Value, a => a.Description);
            string author = GetAttributeValueOrDefault<AssemblyCompanyAttribute>(assembly.Value, a => a.Company);
            if (!builder.Authors.Any() && !String.IsNullOrEmpty(author)) {
                builder.Authors.Add(author);
            }
        }

        private static string GetAttributeValueOrDefault<T>(Assembly assembly, Func<T, string> selector) where T : Attribute {
            // Get the attribute
            T attribute = assembly.GetCustomAttributes(typeof(T), inherit: false).Cast<T>().FirstOrDefault();

            if (attribute != null) {
                string value = selector(attribute);
                // Return the value only if it isn't null or empty so that we can use ?? to fall back
                if (!String.IsNullOrEmpty(value)) {
                    return value;
                }
            }
            return null;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NuGet.Resources;

namespace NuGet
{
    internal static class ManifestSchemaUtility
    {
        /// <summary>
        /// Baseline schema 
        /// </summary>
        internal const string SchemaVersionV1 = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";

        /// <summary>
        /// Added copyrights, references and release notes
        /// </summary>
        internal const string SchemaVersionV2 = "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd";

        /// <summary>
        /// Used if the version is a semantic version.
        /// </summary>
        internal const string SchemaVersionV3 = "http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd";

        /// <summary>
        /// Added 'targetFramework' attribute for 'dependency' elements.
        /// Allow framework folders under 'content' and 'tools' folders. 
        /// </summary>
        internal const string SchemaVersionV4 = "http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd";

        /// <summary>
        /// This is the minimum version of schema that allows specifying
        /// target frameworks for package dependencies.
        /// </summary>
        internal const int TargetFrameworkInDependencyMinVersion = 4;

        private static readonly string[] VersionToSchemaMappings = new[] {
            SchemaVersionV1,
            SchemaVersionV2,
            SchemaVersionV3,
            SchemaVersionV4
        };

        // Mapping from schema to resource name
        private static readonly Dictionary<string, string> SchemaToResourceMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { SchemaVersionV1, "NuGet.Authoring.nuspec.xsd" },
            { SchemaVersionV2, "NuGet.Authoring.nuspec.xsd" },
            { SchemaVersionV3, "NuGet.Authoring.nuspec.xsd" },
            { SchemaVersionV4, "NuGet.Authoring.nuspec.xsd" },
        };

        private static readonly ConcurrentDictionary<string, string> _schemaCache = new ConcurrentDictionary<string, string>(
            concurrencyLevel: 4, capacity: 5, comparer: StringComparer.OrdinalIgnoreCase);

        public static int GetVersionFromNamespace(string @namespace)
        {
            int index = Math.Max(0, Array.IndexOf(VersionToSchemaMappings, @namespace));

            // we count version from 1 instead of 0
            return index + 1;
        }

        public static string GetSchemaNamespace(int version)
        {
            // Versions are internally 0-indexed but stored with a 1 index so decrement it by 1
            if (version <= 0 || version > VersionToSchemaMappings.Length)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.UnknownSchemaVersion, version));
            }
            return VersionToSchemaMappings[version - 1];
        }

        public static TextReader GetSchemaReader(string schemaNamespace)
        {
            string schemaResourceName;
            if (!SchemaToResourceMappings.TryGetValue(schemaNamespace, out schemaResourceName))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_InvalidSchemaNamespace,
                    schemaNamespace));
            }

            string cachedContent = _schemaCache.GetOrAdd(schemaNamespace, _ =>
            {
                // Update the xsd with the right schema namespace
                var assembly = typeof(Manifest).Assembly;
                using (var reader = new StreamReader(assembly.GetManifestResourceStream(schemaResourceName)))
                {
                    string content = reader.ReadToEnd();
                    return String.Format(CultureInfo.InvariantCulture, content, schemaNamespace);
                }
            });

            return new StringReader(cachedContent);
        }

        public static bool IsKnownSchema(string schemaNamespace)
        {
            return SchemaToResourceMappings.ContainsKey(schemaNamespace);
        }
    }
}

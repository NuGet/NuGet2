using System;
using System.Globalization;
using System.Linq;
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
        /// Added 'targetFramework' attribute for 'references' elements.
        /// Added 'minVersion' attribute
        /// </summary>
        internal const string SchemaVersionV5 = "http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd";

        private static readonly string[] VersionToSchemaMappings = new[] {
            SchemaVersionV1,
            SchemaVersionV2,
            SchemaVersionV3,
            SchemaVersionV4,
            SchemaVersionV5,
        };

        public static string GetSchemaNamespace(int version)
        {
            // Versions are internally 0-indexed but stored with a 1 index so decrement it by 1
            if (version <= 0 || version > VersionToSchemaMappings.Length)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.UnknownSchemaVersion, version));
            }
            return VersionToSchemaMappings[version - 1];
        }

        public static bool IsKnownSchema(string schemaNamespace)
        {
            return VersionToSchemaMappings.Contains(schemaNamespace, StringComparer.OrdinalIgnoreCase);
        }
    }
}
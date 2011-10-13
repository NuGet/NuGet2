namespace NuGet
{
    public static class Constants
    {
        public static readonly string PackageExtension = ".nupkg";
        public static readonly string ManifestExtension = ".nuspec";
        public static readonly string ContentDirectory = "content";
        public static readonly string LibDirectory = "lib";
        public static readonly string ToolsDirectory = "tools";
        public static readonly string SettingsFileName = "NuGet.Config";

        internal const string PackageServiceEntitySetName = "Packages";
        internal const string PackageRelationshipNamespace = "http://schemas.microsoft.com/packaging/2010/07/";
    }
}

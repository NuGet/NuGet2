namespace NuGet
{
    /// <summary>
    /// A Property of a file in the manifest.
    /// Used to set property values for a VSProjectItem once added to the project for example
    /// </summary>
    public interface IPackageManifestFileProperty
    {
        string Name { get; }
        string Value { get; }
    }
}
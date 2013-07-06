namespace NuGet.VisualStudio
{
    /// <summary>
    /// <para>Abstraction for methods on a DTE.ProjectItem required for package file processing</para>
    /// </summary>
    public interface IProjectFileProcessingProjectItem
    {
        /// <summary>
        /// <para>Gets the path of the project item</para>
        /// </summary>
        string Path { get; }

        /// <summary>
        /// <para>Set the value of the named property</para>
        /// </summary>
        void SetPropertyValue(string name, string value);
    }
}
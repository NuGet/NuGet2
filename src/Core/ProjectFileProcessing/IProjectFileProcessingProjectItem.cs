namespace NuGet
{
    /// <summary>
    /// Abstraction for methods on a DTE.ProjectItem required for package file processing
    /// </summary>
    public interface IProjectFileProcessingProjectItem
    {
        /// <summary>
        /// Gets the path of the project item
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Set the value of the named property
        /// </summary>
        void SetPropertyValue(string name, string value);

        /// <summary>
        /// Runs the custom tool on the projectItem
        /// </summary>
        void RunCustomTool();
    }
}
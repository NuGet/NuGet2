namespace NuGet
{
    /// <summary>
    /// Abstraction for a file processor
    /// </summary>
    public interface IProjectFileProcessor
    {
        /// <summary>
        /// Check for match given a project item
        /// </summary>
        bool IsMatch(IProjectFileProcessingProjectItem projectItem);

        /// <summary>
        /// Process the project item
        /// </summary>
        void Process(IProjectFileProcessingProjectItem projectItem);
    }
}
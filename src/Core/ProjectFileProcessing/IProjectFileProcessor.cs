namespace NuGet.VisualStudio
{
    /// <summary>
    /// <para>Abstraction for a file processor</para>
    /// </summary>
    public interface IProjectFileProcessor
    {
        /// <summary>
        /// <para>Check for match given a project item</para>
        /// </summary>
        bool IsMatch(IProjectFileProcessingProjectItem projectItem);

        /// <summary>
        /// <para>Process the project item passed</para>
        /// </summary>
        void Process(IProjectFileProcessingProjectItem projectItem);
    }
}
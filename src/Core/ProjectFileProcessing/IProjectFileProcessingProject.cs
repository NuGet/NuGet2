using System.Collections.Generic;

namespace NuGet
{
    /// <summary>
    /// Abstraction for methods on a Project required for package file processing
    /// </summary>
    public interface IProjectFileProcessingProject
    {
        /// <summary>
        /// Get an item from the project given its path
        /// </summary>
        IProjectFileProcessingProjectItem GetItem(string path);

        /// <summary>
        /// Gets processors for a given package
        /// </summary>
        IEnumerable<IProjectFileProcessor> GetProcessorsFromPackage(IPackage package);
    }
}
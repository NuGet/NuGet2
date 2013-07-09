using System.Collections.Generic;

namespace NuGet
{
    /// <summary>
    /// <para>Abstraction for methods on a pProject required for package file processing</para>
    /// </summary>
    public interface IProjectFileProcessingProject
    {
        /// <summary>
        /// <para>Get an item from the project given its path</para>
        /// </summary>
        IProjectFileProcessingProjectItem GetItem(string path);

        /// <summary>
        /// <para>Gets processors for a given package</para>
        /// </summary>
        IEnumerable<IProjectFileProcessor> GetProcessorsFromPackage(IPackage package);
    }
}
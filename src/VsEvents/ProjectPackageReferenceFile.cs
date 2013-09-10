using EnvDTE;

namespace NuGet.VsEvents
{
    /// <summary>
    /// Represents the package reference file, i.e. the pacakges.config file, associated with a project.
    /// </summary>
    internal class ProjectPackageReferenceFile
    {
        public ProjectPackageReferenceFile(Project project, string fullPath)
        {
            Project = project;
            FullPath = fullPath;
        }

        /// <summary>
        /// Gets the project that is associated with the package reference file.
        /// </summary>
        public Project Project { get; private set; }

        /// <summary>
        /// Gets the full path of the package reference file.
        /// </summary>
        public string FullPath { get; private set; }
    }
}

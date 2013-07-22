using System;
using EnvDTE;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// Represents a project name in the solution manager.
    /// </summary>
    internal class ProjectName : IEquatable<ProjectName>
    {
        public ProjectName(Project project)
        {
            FullName = project.FullName;
            UniqueName = project.GetUniqueName();
            ShortName = project.GetName();
            CustomUniqueName = project.GetCustomUniqueName();
        }

        public string FullName { get; private set; }
        public string UniqueName { get; private set; }
        public string ShortName { get; private set; }
        public string CustomUniqueName { get; private set; }

        /// <summary>
        /// Two projects are equal if they share the same FullNames.
        /// </summary>
        public bool Equals(ProjectName other)
        {
            return other.FullName.Equals(other.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        public override string ToString()
        {
            return UniqueName;
        }
    }
}

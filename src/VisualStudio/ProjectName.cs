using System;
using EnvDTE;

namespace NuGet.VisualStudio {
    /// <summary>
    /// Represents a project name in the solution manager.
    /// </summary>
    internal class ProjectName : IEquatable<ProjectName> {
        public ProjectName(Project project) {
            UniqueName = project.UniqueName;
            ShortName = project.Name;
            CustomUniqueName = project.GetCustomUniqueName();
        }

        public string UniqueName { get; private set; }
        public string ShortName { get; private set; }
        public string CustomUniqueName { get; private set; }

        public bool Equals(ProjectName other) {
            return other.UniqueName.Equals(other.UniqueName, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode() {
            return UniqueName.GetHashCode();
        }

        public override string ToString() {
            return UniqueName;
        }
    }
}

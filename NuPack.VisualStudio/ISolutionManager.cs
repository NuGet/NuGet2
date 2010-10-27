using System.Collections.Generic;
using EnvDTE;
using System.Diagnostics.CodeAnalysis;

namespace NuGet.VisualStudio {
    public interface ISolutionManager {
        string DefaultProjectName { get; set; }

        Project GetProject(string projectName);

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This is an expensive operation")]
        IEnumerable<Project> GetProjects();

        bool IsSolutionOpen { get; }
    }
}

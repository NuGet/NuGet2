using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EnvDTE;

namespace NuGet.VisualStudio {
    public interface ISolutionManager {
        event EventHandler SolutionOpened;
        event EventHandler SolutionClosing;

        string DefaultProjectName { get; set; }
        Project DefaultProject { get; }

        Project GetProject(string projectName);

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This is an expensive operation")]
        IEnumerable<Project> GetProjects();

        bool IsSolutionOpen { get; }
    }
}

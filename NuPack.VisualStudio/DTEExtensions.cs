using System;
using EnvDTE;

namespace NuGet.VisualStudio {
    public static class DTEExtensions {
        public static Project GetActiveProject(this _DTE dte) {
            if (dte == null) {
                throw new ArgumentException("dte");
            }

            Project activeProject = null;

            var activeProjects = (object[])dte.ActiveSolutionProjects;
            if (activeProjects != null && activeProjects.Length > 0) {
                Project project = activeProjects[0] as Project;
                if (project != null) {
                    return project;
                }
            }

            return activeProject;
        }
    }
}

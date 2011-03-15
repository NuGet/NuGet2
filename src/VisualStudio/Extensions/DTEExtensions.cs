using System;
using System.Runtime.InteropServices;
using EnvDTE;

namespace NuGet.VisualStudio {
    public static class DTEExtensions {
        public static Project GetActiveProject(this _DTE dte) {
            if (dte == null) {
                throw new ArgumentNullException("dte");
            }

            Project activeProject = null;

            try {
                var activeProjects = (object[])dte.ActiveSolutionProjects;
                if (activeProjects != null && activeProjects.Length > 0) {
                    Project project = activeProjects[0] as Project;
                    if (project != null) {
                        return project;
                    }
                }
            }
            catch (COMException) {
                // accessing ActiveSolutionProjects can result in a COMException if the solution explorer is hidden
            }

            return activeProject;
        }
    }
}

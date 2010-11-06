using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace NuGet.VisualStudio {
    public static class DTEExtensions {
        public static Project GetActiveProject(this _DTE dte) {
            Project activeProject = null;

            if (dte != null) {
                Array activeProjects = dte.ActiveSolutionProjects as Array;
                if (activeProjects != null && activeProjects.Length > 0) {
                    Object projectValue = activeProjects.GetValue(0);
                    Project project = projectValue as Project;
                    if (project != null) {
                        return project;
                    }
                }
            }
            return activeProject;
        }
    }
}

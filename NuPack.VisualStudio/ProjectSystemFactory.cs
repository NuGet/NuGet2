namespace NuPack.VisualStudio {
    using System;
    using System.Globalization;
    using EnvDTE;
    using NuPack.VisualStudio.Resources;

    internal static class ProjectSystemFactory {        
        private static bool IsWebSite(Project project) {
            return project.Kind != null && project.Kind.Equals(VSConstants.WebSiteProjectKind, StringComparison.OrdinalIgnoreCase);
        }

        internal static VSProjectSystem CreateProjectSystem(object projectInstance) {
            Project project = projectInstance as Project;

            if (project == null) {
                throw new InvalidOperationException(VsResources.DTE_InvalidProject);
            }

            if (String.IsNullOrEmpty(project.FullName)) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, 
                    VsResources.DTE_ProjectUnsupported, project.Name));
            }
            
            // Pick a project system based on the type of project
            if (IsWebSite(project)) {
                return new WebSiteProjectSystem(project);
            }

            // If it's not a web site we assume it's a regular VS project
            return new VSProjectSystem(project);
        }
    }
}
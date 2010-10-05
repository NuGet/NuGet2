namespace NuPack.VisualStudio {
    using System;
    using System.Globalization;
    using EnvDTE;
    using NuPack.VisualStudio.Resources;

    internal static class ProjectSystemFactory {                
        internal static VSProjectSystem CreateProjectSystem(Project project) {            
            if (project == null) {
                throw new ArgumentNullException("project");
            }

            if (String.IsNullOrEmpty(project.FullName)) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, 
                    VsResources.DTE_ProjectUnsupported, project.Name));
            }
            
            // Pick a project system based on the type of project
            if (project.IsWebSite()) {
                return new WebSiteProjectSystem(project);
            }

            // If it's not a web site we assume it's a regular VS project
            return new VSProjectSystem(project);
        }
    }
}
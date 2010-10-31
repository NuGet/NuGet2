using System;
using System.Globalization;
using System.Linq;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    public static class ProjectSystemFactory {
        public static ProjectSystem CreateProjectSystem(Project project) {            
            if (project == null) {
                throw new ArgumentNullException("project");
            }

            if (String.IsNullOrEmpty(project.FullName)) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, 
                    VsResources.DTE_ProjectUnsupported, project.Name));
            }
            
            // Websites are special project types so we treat those specially
            if (project.IsWebSite()) {
                return new WebSiteProjectSystem(project);
            }

            if (project.GetProjectTypeGuids().Contains(VsConstants.WebApplicationProjectTypeGuid, StringComparer.OrdinalIgnoreCase)) {
                return new WebProjectSystem(project);
            }

            // If it's not a web site we assume it's a regular VS project
            return new VsProjectSystem(project);
        }
    }
}

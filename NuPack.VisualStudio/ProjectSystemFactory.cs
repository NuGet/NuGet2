using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    public static class ProjectSystemFactory {
        private static Dictionary<string, Func<Project, ProjectSystem>> _factories = new Dictionary<string, Func<Project, ProjectSystem>>(StringComparer.OrdinalIgnoreCase) {
            { VsConstants.WebApplicationProjectTypeGuid , project => new WebProjectSystem(project) },
            { VsConstants.WebSiteProjectTypeGuid , project => new WebSiteProjectSystem(project) },
        };


        public static ProjectSystem CreateProjectSystem(Project project) {            
            if (project == null) {
                throw new ArgumentNullException("project");
            }

            if (String.IsNullOrEmpty(project.FullName)) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, 
                    VsResources.DTE_ProjectUnsupported, project.Name));
            }

            // Try to get a factory for the project type guid            
            foreach (var guid in project.GetProjectTypeGuids()) {
                Func<Project, ProjectSystem> factory;
                if (_factories.TryGetValue(guid, out factory)) {
                    return factory(project);
                }
            }

            // Fall back to the default if we have no special project types
            return new VsProjectSystem(project);
        }
    }
}

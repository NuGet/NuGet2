using System;
using System.Collections.Generic;
using System.Globalization;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    public static class VsProjectSystemFactory {
        private static Dictionary<string, Func<Project, IProjectSystem>> _factories = new Dictionary<string, Func<Project, IProjectSystem>>(StringComparer.OrdinalIgnoreCase) {
            { VsConstants.WebApplicationProjectTypeGuid , project => new WebProjectSystem(project) },
            { VsConstants.WebSiteProjectTypeGuid , project => new WebSiteProjectSystem(project) },
            { VsConstants.FsharpProjectTypeGuid , project => new FSharpProjectSystem(project) },
            { VsConstants.WixProjectTypeGuid , project => new WixProjectSystem(project) }
        };


        public static IProjectSystem CreateProjectSystem(Project project) {
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
                Func<Project, IProjectSystem> factory;
                if (_factories.TryGetValue(guid, out factory)) {
                    return factory(project);
                }
            }

            // Fall back to the default if we have no special project types
            return new VsProjectSystem(project);
        }
    }
}
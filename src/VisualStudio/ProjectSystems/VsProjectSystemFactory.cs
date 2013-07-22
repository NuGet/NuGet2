using System;
using System.Collections.Generic;
using System.Globalization;
using EnvDTE;
using NuGet.VisualStudio.Resources;
using ProjectThunk = System.Func<EnvDTE.Project, NuGet.VisualStudio.IFileSystemProvider, NuGet.IProjectSystem>;

namespace NuGet.VisualStudio
{
    public static class VsProjectSystemFactory
    {
        private static Dictionary<string, ProjectThunk> _factories = new Dictionary<string, ProjectThunk>(StringComparer.OrdinalIgnoreCase) {
            { VsConstants.WebApplicationProjectTypeGuid, (project, fileSystemProvider) => new WebProjectSystem(project, fileSystemProvider) },
            { VsConstants.WebSiteProjectTypeGuid, (project, fileSystemProvider) => new WebSiteProjectSystem(project, fileSystemProvider) },
            { VsConstants.CppProjectTypeGuid, (project, fileSystemProvider) => new NativeProjectSystem(project, fileSystemProvider) },
            { VsConstants.FsharpProjectTypeGuid, (project, fileSystemProvider) => new FSharpProjectSystem(project, fileSystemProvider) },
            { VsConstants.WixProjectTypeGuid, (project, fileSystemProvider) => new WixProjectSystem(project, fileSystemProvider) },
            { VsConstants.JsProjectTypeGuid, (project, fileSystemProvider) => new JsProjectSystem(project, fileSystemProvider) },
            { VsConstants.WindowsStoreProjectTypeGuid, (project, fileSystemProvider) => new WindowsStoreProjectSystem(project, fileSystemProvider) }
        };

        public static IProjectSystem CreateProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (String.IsNullOrEmpty(project.FullName))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.DTE_ProjectUnsupported, project.GetName()));
            }

            // Try to get a factory for the project type guid            
            foreach (var guid in project.GetProjectTypeGuids())
            {
                ProjectThunk factory;
                if (_factories.TryGetValue(guid, out factory))
                {
                    return factory(project, fileSystemProvider);
                }
            }

            // Fall back to the default if we have no special project types
            return new VsProjectSystem(project, fileSystemProvider);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
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
            { VsConstants.FsharpProjectTypeGuid, (project, fileSystemProvider) => new FSharpProjectSystem(project, fileSystemProvider) },
            { VsConstants.WixProjectTypeGuid, (project, fileSystemProvider) => new WixProjectSystem(project, fileSystemProvider) },
            { VsConstants.JsProjectTypeGuid, (project, fileSystemProvider) => new JsProjectSystem(project, fileSystemProvider) },
            { VsConstants.WindowsStoreProjectTypeGuid, (project, fileSystemProvider) => new WindowsStoreProjectSystem(project, fileSystemProvider) },
            { VsConstants.DeploymentProjectTypeGuid, (project, fileSystemProvider) => new VsProjectSystem(project, fileSystemProvider) }
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

#if VS14
            if (project.SupportsINuGetProjectSystem())
            {
                return new NuGetAwareProjectSystem(project);
            }
#endif


            var guids = project.GetProjectTypeGuids();
            if (guids.Contains(VsConstants.CppProjectTypeGuid)) // Got a cpp project
            {
                var vcx = new VcxProject(project.FullName);
                if (!vcx.HasClrSupport(project.ConfigurationManager.ActiveConfiguration))
                    return new NativeProjectSystem(project, fileSystemProvider);
            }



            // Try to get a factory for the project type guid            
            foreach (var guid in guids)
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


    public interface ILoader
    {
        XDocument LoadXml(string filename);
    }

    public class Loader : ILoader
    {
        static Loader()
        {
           Instance = new Loader();  
        }
        public static ILoader Instance { get; set; }

        public XDocument LoadXml(string filename)
        {
            return XDocument.Load(filename);
        }
    }
}
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

    public class VcxProject
    {

        private readonly XDocument vcxFile;
        public VcxProject(string fullname)
        {
            vcxFile = XDocument.Load(fullname);
        }

        public bool HasClrSupport(Configuration config)
        {
            string filter = config.ConfigurationName + "|" + config.PlatformName;
            var elements = vcxFile.Descendants().Where(x => x.Name.LocalName == "PropertyGroup");
            var actuals1 =
                elements.Where(x => x.Attribute("Label") != null && x.Attribute("Label").Value == "Configuration");

            var actuals2 =
                actuals1.Where(x => x.Attribute("Condition") != null && x.Attribute("Condition").Value.Contains(filter));
            var items = actuals2.Elements().Where(e => e.Name.LocalName == "CLRSupport");
            if (items.Any())
            {
                var clr = items.First();
                return clr.Value != "false";
            }
            return false;
        }


    }
}
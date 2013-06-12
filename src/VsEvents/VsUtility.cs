using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using VsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace NuGet.VsEvents
{
    // The code inside this class is copied from file ProjectExtensions. 
    // The reason of code duplication is to delay loading NuGet.VisualStudio.dll 
    // as long as posssible.
    internal static class VsUtility
    {   
        private static readonly HashSet<string> _supportedProjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        {
            VsConstants.WebSiteProjectTypeGuid, 
            VsConstants.CsharpProjectTypeGuid, 
            VsConstants.VbProjectTypeGuid,
            VsConstants.CppProjectTypeGuid,
            VsConstants.JsProjectTypeGuid,
            VsConstants.FsharpProjectTypeGuid,
            VsConstants.NemerleProjectTypeGuid,
            VsConstants.WixProjectTypeGuid,
            VsConstants.SynergexProjectTypeGuid,
            VsConstants.NomadForVisualStudioProjectTypeGuid 
        };

        public static string GetFullPath(Project project)
        {
            string fullPath = GetPropertyValue<string>(project, "FullPath");
            if (!String.IsNullOrEmpty(fullPath))
            {
                // Some Project System implementations (JS metro app) return the project 
                // file as FullPath. We only need the parent directory
                if (File.Exists(fullPath))
                {
                    fullPath = Path.GetDirectoryName(fullPath);
                }
            }
            else
            {
                // C++ projects do not have FullPath property, but do have ProjectDirectory one.
                fullPath = GetPropertyValue<string>(project, "ProjectDirectory");
            }

            return fullPath;
        }

        public static bool IsSupported(Project project)
        {
            return project.Kind != null && _supportedProjectTypes.Contains(project.Kind);
        }

        public static T GetPropertyValue<T>(Project project, string propertyName)
        {
            if (project.Properties == null)
            {
                // this happens in unit tests
                return default(T);
            }

            try
            {
                Property property = project.Properties.Item(propertyName);
                if (property != null)
                {
                    // REVIEW: Should this cast or convert?
                    return (T)property.Value;
                }
            }
            catch (ArgumentException)
            {
            }
            return default(T);
        }
    }
}

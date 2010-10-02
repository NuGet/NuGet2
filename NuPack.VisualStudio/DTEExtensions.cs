using System;
using System.Diagnostics;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace NuPack.VisualStudio {
    public static class DTEExtensions {

        public static DTE DTE { 
            get; set; 
        }

        public static T GetService<T>(this _DTE dte, Type serviceType) 
            where T : class {
            // Get the service provider from dte            
            return (T)dte.GetServiceProvider().GetService(serviceType);
        }

        public static IServiceProvider GetServiceProvider(this _DTE dte) {
            IServiceProvider serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            Debug.Assert(serviceProvider != null, "Service provider is null");
            return serviceProvider;
        }

        public static EnvDTE.Project GetActiveProject(this EnvDTE._DTE dte) {
            EnvDTE.Project activeProject = null;

            if (dte != null) {
                Object obj = dte.ActiveSolutionProjects;
                if (obj != null && obj is Array && ((Array)obj).Length > 0) {
                    Object proj = ((Array)obj).GetValue(0);

                    if (proj != null && proj is EnvDTE.Project) {
                        activeProject = (EnvDTE.Project)proj;
                    }
                }
            }
            return activeProject;
        }
    }
}
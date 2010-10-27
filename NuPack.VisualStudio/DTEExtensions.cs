using System;
using System.Diagnostics;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace NuGet.VisualStudio {
    public static class DTEExtensions {

        public static DTE DTE { 
            get; set; 
        }

        public static T GetService<T>(this _DTE dte, Type serviceType) 
            where T : class {
            // Get the service provider from dte            
            return (T)dte.GetServiceProvider().GetService(serviceType);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability", 
            "CA2000:Dispose objects before losing scope",
            Justification="We can't dispose an object if we want to return it.")]
        public static IServiceProvider GetServiceProvider(this _DTE dte) {
            IServiceProvider serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            Debug.Assert(serviceProvider != null, "Service provider is null");
            return serviceProvider;
        }

        public static EnvDTE.Project GetActiveProject(this EnvDTE._DTE dte) {
            EnvDTE.Project activeProject = null;

            if (dte != null) {
                Array activeProjects = dte.ActiveSolutionProjects as Array;
                if (activeProjects != null && activeProjects.Length > 0) {
                    Object projectValue = activeProjects.GetValue(0);
                    Project project = projectValue as Project;
                    if (project != null) {
                        return project;
                    }
                }
            }
            return activeProject;
        }
    }
}

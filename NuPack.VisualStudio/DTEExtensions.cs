using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using VsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace NuGet.VisualStudio {
    public static class DTEExtensions {

        public static DTE DTE {
            get;
            set;
        }

        public static T GetService<T>(this _DTE dte, Type serviceType)
            where T : class {
            // Get the service provider from dte            
            return (T)dte.GetServiceProvider().GetService(serviceType);
        }

        public static TService QueryService<TService>(this _DTE dte) {
            Guid guidService = typeof(TService).GUID;
            Guid riid = guidService;
            var serviceProvider = dte as VsServiceProvider;

            IntPtr servicePtr;
            int hr = serviceProvider.QueryService(ref guidService, ref riid, out servicePtr);
            
            if (hr != VsConstants.S_OK) {
                Marshal.ThrowExceptionForHR(hr);
            }
            
            TService service = default(TService);

            if (servicePtr != IntPtr.Zero) {
                service = (TService)Marshal.GetObjectForIUnknown(servicePtr);
                Marshal.Release(servicePtr);
            }

            return service;

        }

        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "We can't dispose an object if we want to return it.")]
        public static IServiceProvider GetServiceProvider(this _DTE dte) {
            IServiceProvider serviceProvider = new ServiceProvider(dte as VsServiceProvider);
            Debug.Assert(serviceProvider != null, "Service provider is null");
            return serviceProvider;
        }


        public static Project GetActiveProject(this _DTE dte) {
            Project activeProject = null;

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
